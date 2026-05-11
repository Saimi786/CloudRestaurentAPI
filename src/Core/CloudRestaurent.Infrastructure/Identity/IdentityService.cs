using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Infrastructure.Identity;

public sealed class IdentityService(UserManager<AppUser> userManager, AppDbContext db) : IIdentityService
{
    public async Task<AuthenticatedUser?> ValidateCredentialsAsync(string email, string password, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive) return null;
        if (!await userManager.CheckPasswordAsync(user, password)) return null;

        var roles = await userManager.GetRolesAsync(user);
        var branchIds = await LoadBranchIdsAsync(user.Id, ct);

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        return new AuthenticatedUser(
            user.Id, user.Email!, user.FullName, user.TenantId,
            roles.ToList(), branchIds, user.MaxDiscountPercent);
    }

    public async Task<IReadOnlyList<UserSummary>> ListUsersAsync(Guid tenantId, bool includeInactive, CancellationToken ct)
    {
        var query = userManager.Users.Where(u => u.TenantId == tenantId);
        if (!includeInactive) query = query.Where(u => u.IsActive);

        var users = await query.OrderBy(u => u.Email).ToListAsync(ct);
        var userIds = users.Select(u => u.Id).ToList();

        // Single round-trip for everyone's branch assignments rather than N+1.
        var branchMap = await db.UserBranches.AsNoTracking()
            .Where(ub => userIds.Contains(ub.UserId))
            .GroupBy(ub => ub.UserId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(x => x.BranchId).ToList(), ct);

        var result = new List<UserSummary>(users.Count);
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            var branchIds = branchMap.TryGetValue(u.Id, out var list) ? (IReadOnlyList<Guid>)list : Array.Empty<Guid>();
            result.Add(ToSummary(u, roles, branchIds));
        }
        return result;
    }

    public async Task<UserSummary?> GetUserAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId, ct);
        if (user is null) return null;
        var roles = await userManager.GetRolesAsync(user);
        var branchIds = await LoadBranchIdsAsync(user.Id, ct);
        return ToSummary(user, roles, branchIds);
    }

    public async Task<UserSummary> CreateUserAsync(CreateUserInput input, CancellationToken ct)
    {
        // Validate roles BEFORE creating the user — otherwise an invalid role
        // leaves an orphan account in the DB after the role-assignment throws.
        var validatedRoles = input.Roles.Count > 0
            ? ValidateRoles(input.Roles)
            : Array.Empty<string>();

        if (await userManager.FindByEmailAsync(input.Email) is not null)
            throw new IdentityOperationException(
                $"A user with email '{input.Email}' already exists.");

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = input.Email,
            Email = input.Email,
            EmailConfirmed = true,
            FullName = input.FullName,
            TenantId = input.TenantId,
            IsActive = true,
            MaxDiscountPercent = input.MaxDiscountPercent
        };

        var create = await userManager.CreateAsync(user, input.Password);
        if (!create.Succeeded) throw FromIdentityResult(create);

        if (validatedRoles.Length > 0)
        {
            var add = await userManager.AddToRolesAsync(user, validatedRoles);
            if (!add.Succeeded) throw FromIdentityResult(add);
        }

        if (input.BranchIds.Count > 0)
            await SetBranchAssignmentsAsync(user.Id, input.TenantId, input.BranchIds, ct);

        var assignedRoles = await userManager.GetRolesAsync(user);
        var branchIds = await LoadBranchIdsAsync(user.Id, ct);
        return ToSummary(user, assignedRoles, branchIds);
    }

    public async Task<UserSummary> UpdateUserAsync(
        Guid id, Guid tenantId, UpdateUserInput input, CancellationToken ct)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId, ct)
            ?? throw new IdentityOperationException("User not found in this tenant.");

        user.FullName = input.FullName;
        user.IsActive = input.IsActive;
        user.MaxDiscountPercent = input.MaxDiscountPercent;
        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded) throw FromIdentityResult(update);

        var current = await userManager.GetRolesAsync(user);
        var desired = ValidateRoles(input.Roles);

        var toRemove = current.Except(desired).ToList();
        var toAdd = desired.Except(current).ToList();

        if (toRemove.Count > 0)
        {
            var r = await userManager.RemoveFromRolesAsync(user, toRemove);
            if (!r.Succeeded) throw FromIdentityResult(r);
        }
        if (toAdd.Count > 0)
        {
            var a = await userManager.AddToRolesAsync(user, toAdd);
            if (!a.Succeeded) throw FromIdentityResult(a);
        }

        await SetBranchAssignmentsAsync(user.Id, tenantId, input.BranchIds, ct);

        var finalRoles = await userManager.GetRolesAsync(user);
        var branchIds = await LoadBranchIdsAsync(user.Id, ct);
        return ToSummary(user, finalRoles, branchIds);
    }

    public async Task ResetPasswordAsync(Guid id, Guid tenantId, string newPassword, CancellationToken ct)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId, ct)
            ?? throw new IdentityOperationException("User not found in this tenant.");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded) throw FromIdentityResult(result);
    }

    public async Task DeactivateUserAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId, ct)
            ?? throw new IdentityOperationException("User not found in this tenant.");

        if (!user.IsActive) return;
        user.IsActive = false;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) throw FromIdentityResult(result);
    }

    public async Task SetBranchAssignmentsAsync(
        Guid userId, Guid tenantId, IReadOnlyList<Guid> branchIds, CancellationToken ct)
    {
        var existing = await db.UserBranches
            .Where(ub => ub.UserId == userId)
            .ToListAsync(ct);
        var desired = branchIds.Distinct().ToHashSet();

        // Remove rows no longer wanted.
        var toRemove = existing.Where(ub => !desired.Contains(ub.BranchId)).ToList();
        if (toRemove.Count > 0) db.UserBranches.RemoveRange(toRemove);

        // Add new rows.
        var current = existing.Select(ub => ub.BranchId).ToHashSet();
        foreach (var bid in desired.Where(b => !current.Contains(b)))
        {
            db.UserBranches.Add(new UserBranch
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                BranchId = bid
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public IReadOnlyList<string> GetAssignableRoles() =>
        AppRoles.All.Where(r => r != AppRoles.SuperAdmin).ToArray();

    // --- helpers ---

    private Task<List<Guid>> LoadBranchIdsAsync(Guid userId, CancellationToken ct) =>
        db.UserBranches.AsNoTracking()
            .Where(ub => ub.UserId == userId)
            .Select(ub => ub.BranchId)
            .ToListAsync(ct);

    private static UserSummary ToSummary(AppUser u, IList<string> roles, IReadOnlyList<Guid> branchIds) =>
        new(u.Id, u.Email!, u.FullName, u.IsActive, u.CreatedAt, u.LastLoginAt,
            roles.ToList(), branchIds, u.MaxDiscountPercent);

    private string[] ValidateRoles(IEnumerable<string> roles)
    {
        var assignable = GetAssignableRoles().ToHashSet(StringComparer.Ordinal);
        var requested = roles.Distinct(StringComparer.Ordinal).ToArray();
        var unknown = requested.Where(r => !assignable.Contains(r)).ToArray();
        if (unknown.Length > 0)
            throw new IdentityOperationException(
                $"Unknown or non-assignable role(s): {string.Join(", ", unknown)}.");
        return requested;
    }

    private static IdentityOperationException FromIdentityResult(IdentityResult result)
    {
        var errors = result.Errors
            .GroupBy(e => e.Code, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Description).ToArray(),
                StringComparer.Ordinal);
        var msg = string.Join("; ", result.Errors.Select(e => e.Description));
        return new IdentityOperationException(msg, errors);
    }
}
