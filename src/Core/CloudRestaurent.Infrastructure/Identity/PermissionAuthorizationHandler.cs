using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace CloudRestaurent.Infrastructure.Identity;

/// <summary>
/// Resolves the caller's roles → permissions via the RolePermission table, with a short
/// in-memory cache (60s) to avoid hitting the DB on every request. Returns success if any
/// of the requirement's listed permissions is granted by any of the user's roles.
/// </summary>
public sealed class PermissionAuthorizationHandler(
    IServiceScopeFactory scopeFactory,
    IMemoryCache cache)
    : AuthorizationHandler<PermissionRequirement>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var roles = context.User
            .FindAll(ClaimTypes.Role).Select(c => c.Value)
            .Concat(context.User.FindAll("role").Select(c => c.Value))
            .Distinct().ToList();
        if (roles.Count == 0) return;

        // SuperAdmin has every permission unconditionally.
        if (roles.Contains(AppRoles.SuperAdmin))
        {
            context.Succeed(requirement);
            return;
        }

        var granted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var role in roles)
        {
            var perms = await cache.GetOrCreateAsync($"perms:{role}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheTtl;
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Persistence.AppDbContext>();
                var roleRow = await db.Roles
                    .FirstOrDefaultAsync(r => r.Name == role);
                if (roleRow is null) return Array.Empty<string>();
                return await db.Set<RolePermission>().AsNoTracking()
                    .Where(rp => rp.RoleId == roleRow.Id)
                    .Select(rp => rp.Permission)
                    .ToArrayAsync();
            });
            if (perms is null) continue;
            foreach (var p in perms) granted.Add(p);
        }

        if (requirement.Permissions.Any(p => granted.Contains(p)))
            context.Succeed(requirement);
    }
}
