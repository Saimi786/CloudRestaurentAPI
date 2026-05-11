using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Infrastructure.Identity;

public sealed class RoleAdminService(
    RoleManager<AppRole> roleManager,
    UserManager<AppUser> userManager,
    AppDbContext db) : IRoleAdminService
{
    private static readonly HashSet<string> BuiltInNames =
        new(AppRoles.All, StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<RoleDetailsDto>> ListAsync(Guid tenantId, CancellationToken ct)
    {
        // Built-in roles have TenantId=null and are shared; custom roles are tagged
        // with this tenant. We surface both — UI prevents editing built-in names.
        var roles = await roleManager.Roles
            .Where(r => r.TenantId == null || r.TenantId == tenantId)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

        var roleIds = roles.Select(r => r.Id).ToList();
        var permsByRole = (await db.RolePermissions.AsNoTracking()
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => new { rp.RoleId, rp.Permission })
                .ToListAsync(ct))
            .GroupBy(x => x.RoleId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Permission).OrderBy(p => p).ToList());

        // User counts in one round-trip.
        var userCounts = new Dictionary<Guid, int>();
        foreach (var r in roles)
            userCounts[r.Id] = (await userManager.GetUsersInRoleAsync(r.Name!)).Count;

        return roles.Select(r => new RoleDetailsDto(
            r.Id,
            r.Name!,
            BuiltInNames.Contains(r.Name!),
            userCounts.GetValueOrDefault(r.Id),
            permsByRole.TryGetValue(r.Id, out var p)
                ? (IReadOnlyList<string>)p
                : Array.Empty<string>())).ToList();
    }

    public async Task<RoleDetailsDto> CreateAsync(
        Guid tenantId, string name, IReadOnlyList<string> permissions, CancellationToken ct)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleException("Role name is required.");
        if (BuiltInNames.Contains(name))
            throw new ConflictException($"'{name}' is a reserved built-in role name.");
        if (await roleManager.Roles.AnyAsync(r => r.Name == name && (r.TenantId == null || r.TenantId == tenantId), ct))
            throw new ConflictException($"A role named '{name}' already exists.");

        var validated = ValidatePermissions(permissions);

        var role = new AppRole(name) { Id = Guid.NewGuid(), TenantId = tenantId };
        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded) throw FromIdentityResult(result);

        await ReplacePermissionsAsync(role.Id, validated, ct);

        return new RoleDetailsDto(role.Id, role.Name!, IsBuiltIn: false, UserCount: 0, validated);
    }

    public async Task<RoleDetailsDto> UpdateAsync(
        Guid tenantId, Guid roleId, string name, IReadOnlyList<string> permissions, CancellationToken ct)
    {
        var role = await roleManager.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && (r.TenantId == null || r.TenantId == tenantId), ct)
            ?? throw new NotFoundException("Role", roleId);

        var isBuiltIn = BuiltInNames.Contains(role.Name!);
        name = name.Trim();

        // Built-in roles: name is fixed (it's how authorization claims line up with
        // AppRoles.SuperAdmin etc.). Permissions remain editable so admins can tune them.
        if (!isBuiltIn)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new BusinessRuleException("Role name is required.");
            if (BuiltInNames.Contains(name))
                throw new ConflictException($"'{name}' is a reserved built-in role name.");
            if (!string.Equals(role.Name, name, StringComparison.OrdinalIgnoreCase) &&
                await roleManager.Roles.AnyAsync(r => r.Name == name && r.Id != role.Id, ct))
                throw new ConflictException($"A role named '{name}' already exists.");

            role.Name = name;
            role.NormalizedName = roleManager.NormalizeKey(name);
            var update = await roleManager.UpdateAsync(role);
            if (!update.Succeeded) throw FromIdentityResult(update);
        }

        var validated = ValidatePermissions(permissions);
        await ReplacePermissionsAsync(role.Id, validated, ct);

        var userCount = (await userManager.GetUsersInRoleAsync(role.Name!)).Count;
        return new RoleDetailsDto(role.Id, role.Name!, isBuiltIn, userCount, validated);
    }

    public async Task DeleteAsync(Guid tenantId, Guid roleId, CancellationToken ct)
    {
        var role = await roleManager.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.TenantId == tenantId, ct)
            ?? throw new NotFoundException("Role", roleId);

        if (BuiltInNames.Contains(role.Name!))
            throw new BusinessRuleException("Built-in roles cannot be deleted.");

        var inUse = (await userManager.GetUsersInRoleAsync(role.Name!)).Count;
        if (inUse > 0)
            throw new ConflictException($"Role is still assigned to {inUse} user(s). Reassign them first.");

        // Cascade the perms ourselves; AspNetRoles delete doesn't know about RolePermissions.
        var perms = await db.RolePermissions.Where(rp => rp.RoleId == role.Id).ToListAsync(ct);
        if (perms.Count > 0) db.RolePermissions.RemoveRange(perms);
        await db.SaveChangesAsync(ct);

        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded) throw FromIdentityResult(result);
    }

    public IReadOnlyList<PermissionDescriptor> GetPermissionCatalog() =>
        AppPermissions.All
            .Select(p => new PermissionDescriptor(p, p.Split('.', 2)[0]))
            .OrderBy(p => p.Area, StringComparer.Ordinal)
            .ThenBy(p => p.Key, StringComparer.Ordinal)
            .ToList();

    // ---------- helpers ----------

    private async Task ReplacePermissionsAsync(Guid roleId, IReadOnlyList<string> permissions, CancellationToken ct)
    {
        var existing = await db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync(ct);
        var wanted = permissions.ToHashSet(StringComparer.Ordinal);

        var toRemove = existing.Where(rp => !wanted.Contains(rp.Permission)).ToList();
        if (toRemove.Count > 0) db.RolePermissions.RemoveRange(toRemove);

        var have = existing.Select(rp => rp.Permission).ToHashSet(StringComparer.Ordinal);
        foreach (var p in wanted.Where(p => !have.Contains(p)))
        {
            db.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = roleId,
                Permission = p
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static IReadOnlyList<string> ValidatePermissions(IReadOnlyList<string> permissions)
    {
        var catalog = AppPermissions.All.ToHashSet(StringComparer.Ordinal);
        var unique = permissions.Distinct(StringComparer.Ordinal).ToList();
        var unknown = unique.Where(p => !catalog.Contains(p)).ToList();
        if (unknown.Count > 0)
            throw new BusinessRuleException(
                $"Unknown permission(s): {string.Join(", ", unknown)}.");
        return unique;
    }

    private static IdentityOperationException FromIdentityResult(IdentityResult r) =>
        new(string.Join("; ", r.Errors.Select(e => e.Description)));
}
