using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Domain.Tenants;
using CloudRestaurent.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Infrastructure.Identity;

public sealed class PlatformTenantService(
    AppDbContext db,
    UserManager<AppUser> userManager) : IPlatformTenantService
{
    public async Task<IReadOnlyList<PlatformTenantListItem>> ListAsync(bool includeInactive, CancellationToken ct)
    {
        // Tenants aren't ITenantScoped so no filter — but Companies/Branches/AppUsers are.
        // We aggregate counts via IgnoreQueryFilters so SuperAdmin sees totals across the
        // whole platform regardless of the JWT's tid claim.
        var query = db.Tenants.AsNoTracking();
        if (!includeInactive) query = query.Where(t => t.IsActive);

        var tenants = await query.OrderBy(t => t.Name).ToListAsync(ct);
        if (tenants.Count == 0) return Array.Empty<PlatformTenantListItem>();

        var ids = tenants.Select(t => t.Id).ToList();

        var companyCounts = await db.Companies.IgnoreQueryFilters().AsNoTracking()
            .Where(c => ids.Contains(c.TenantId))
            .GroupBy(c => c.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, ct);

        var branchCounts = await db.Branches.IgnoreQueryFilters().AsNoTracking()
            .Where(b => ids.Contains(b.TenantId))
            .GroupBy(b => b.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, ct);

        var userCounts = await userManager.Users.AsNoTracking()
            .Where(u => ids.Contains(u.TenantId))
            .GroupBy(u => u.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, ct);

        return tenants.Select(t => new PlatformTenantListItem(
            t.Id, t.Name, t.Slug, (int)t.BusinessType, (int)t.Plan, t.IsActive, t.LogoUrl, t.CreatedAt,
            companyCounts.GetValueOrDefault(t.Id),
            branchCounts.GetValueOrDefault(t.Id),
            userCounts.GetValueOrDefault(t.Id))).ToList();
    }

    public async Task<PlatformTenantDetails> GetAsync(Guid tenantId, CancellationToken ct)
    {
        var t = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tenantId, ct)
            ?? throw new NotFoundException("Tenant", tenantId);

        var companyCount = await db.Companies.IgnoreQueryFilters().AsNoTracking()
            .CountAsync(c => c.TenantId == tenantId, ct);
        var branchCount = await db.Branches.IgnoreQueryFilters().AsNoTracking()
            .CountAsync(b => b.TenantId == tenantId, ct);
        var userCount = await userManager.Users.AsNoTracking()
            .CountAsync(u => u.TenantId == tenantId, ct);

        var adminEmail = await userManager.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.CreatedAt)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(ct);

        return new PlatformTenantDetails(
            t.Id, t.Name, t.Slug, (int)t.BusinessType, (int)t.Plan, t.IsActive, t.LogoUrl, t.CreatedAt,
            companyCount, branchCount, userCount, adminEmail);
    }

    public async Task<PlatformTenantDetails> CreateAsync(CreatePlatformTenantInput input, CancellationToken ct)
    {
        var slug = input.Slug.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slug))
            throw new BusinessRuleException("Slug is required.");
        if (await db.Tenants.AsNoTracking().AnyAsync(t => t.Slug == slug, ct))
            throw new ConflictException($"A tenant with slug '{slug}' already exists.");

        if (!Enum.IsDefined(typeof(BusinessType), input.BusinessType))
            throw new BusinessRuleException($"Invalid business type: {input.BusinessType}.");
        if (!Enum.IsDefined(typeof(SubscriptionPlan), input.Plan))
            throw new BusinessRuleException($"Invalid plan: {input.Plan}.");

        if (await userManager.FindByEmailAsync(input.AdminEmail) is not null)
            throw new ConflictException($"A user with email '{input.AdminEmail}' already exists.");

        // 1) Tenant row
        var tenant = new Tenant(
            Guid.NewGuid(), input.Name.Trim(), slug,
            (BusinessType)input.BusinessType, (SubscriptionPlan)input.Plan);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);

        // 2) Seed initial TenantAdmin user. If user creation fails we roll back the
        // tenant so we don't leave an orphan empty tenant in the DB.
        var admin = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = input.AdminEmail,
            Email = input.AdminEmail,
            EmailConfirmed = true,
            FullName = input.AdminFullName,
            TenantId = tenant.Id,
            IsActive = true
        };
        var create = await userManager.CreateAsync(admin, input.AdminPassword);
        if (!create.Succeeded)
        {
            db.Tenants.Remove(tenant);
            await db.SaveChangesAsync(ct);
            throw new IdentityOperationException(
                string.Join("; ", create.Errors.Select(e => e.Description)));
        }

        var addRole = await userManager.AddToRoleAsync(admin, AppRoles.TenantAdmin);
        if (!addRole.Succeeded)
        {
            await userManager.DeleteAsync(admin);
            db.Tenants.Remove(tenant);
            await db.SaveChangesAsync(ct);
            throw new IdentityOperationException(
                string.Join("; ", addRole.Errors.Select(e => e.Description)));
        }

        return await GetAsync(tenant.Id, ct);
    }

    public async Task<PlatformTenantDetails> UpdateAsync(
        Guid tenantId, UpdatePlatformTenantInput input, CancellationToken ct)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new NotFoundException("Tenant", tenantId);

        if (!Enum.IsDefined(typeof(SubscriptionPlan), input.Plan))
            throw new BusinessRuleException($"Invalid plan: {input.Plan}.");

        // No Rename method on the entity yet — use reflection-safe write through
        // private setter via the existing methods. ChangePlan exists; for name we
        // need a small entity method (added below). For now, use property write.
        tenant.Rename(input.Name.Trim());
        tenant.ChangePlan((SubscriptionPlan)input.Plan);
        await db.SaveChangesAsync(ct);

        return await GetAsync(tenant.Id, ct);
    }

    public async Task SetActiveAsync(Guid tenantId, bool isActive, CancellationToken ct)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new NotFoundException("Tenant", tenantId);
        if (isActive) tenant.Activate(); else tenant.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
