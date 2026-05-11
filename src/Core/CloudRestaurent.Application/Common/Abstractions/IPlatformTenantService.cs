namespace CloudRestaurent.Application.Common.Abstractions;

/// <summary>
/// Platform-level (SuperAdmin) tenant administration. Every method bypasses
/// the per-tenant query filter — these calls intentionally cross the tenant
/// boundary, which only callers with <c>Platform.ManageTenants</c> may do.
///
/// Tenant CRUD lives here (not in the regular Tenancy module) because creation
/// also seeds an initial admin <see cref="AppUser"/>, which requires
/// <see cref="IIdentityService"/> + the ASP.NET Identity <c>UserManager</c>.
/// </summary>
public interface IPlatformTenantService
{
    Task<IReadOnlyList<PlatformTenantListItem>> ListAsync(bool includeInactive, CancellationToken ct);
    Task<PlatformTenantDetails> GetAsync(Guid tenantId, CancellationToken ct);

    Task<PlatformTenantDetails> CreateAsync(CreatePlatformTenantInput input, CancellationToken ct);
    Task<PlatformTenantDetails> UpdateAsync(Guid tenantId, UpdatePlatformTenantInput input, CancellationToken ct);
    Task SetActiveAsync(Guid tenantId, bool isActive, CancellationToken ct);
}

public sealed record PlatformTenantListItem(
    Guid Id,
    string Name,
    string Slug,
    int BusinessType,
    int Plan,
    bool IsActive,
    string? LogoUrl,
    DateTimeOffset CreatedAt,
    int CompanyCount,
    int BranchCount,
    int UserCount);

public sealed record PlatformTenantDetails(
    Guid Id,
    string Name,
    string Slug,
    int BusinessType,
    int Plan,
    bool IsActive,
    string? LogoUrl,
    DateTimeOffset CreatedAt,
    int CompanyCount,
    int BranchCount,
    int UserCount,
    string? AdminEmail);

public sealed record CreatePlatformTenantInput(
    string Name,
    string Slug,
    int BusinessType,
    int Plan,
    string AdminEmail,
    string AdminFullName,
    string AdminPassword);

public sealed record UpdatePlatformTenantInput(
    string Name,
    int Plan);
