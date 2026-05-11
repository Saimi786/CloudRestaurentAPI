using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Domain.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tenancy.Application.Platform;

/// <summary>
/// SuperAdmin "Manage Location" page — full branch + parent tenant + first-admin-user
/// info in one place. Cross-tenant (uses IgnoreQueryFilters) since SuperAdmin doesn't
/// necessarily belong to the target tenant.
/// </summary>
public sealed record GetPlatformBranchQuery(Guid BranchId)
    : IRequest<PlatformBranchDetailDto>;

public sealed record PlatformBranchDetailDto(
    // Tenant
    Guid TenantId,
    string TenantName,
    int TenantBusinessType,
    int TenantPlan,
    string? TenantLogoUrl,
    string? OwnerEmail,
    // Company
    Guid CompanyId,
    string CompanyName,
    string CompanyLegalName,
    // Branch
    Guid BranchId,
    string BranchName,
    string BranchCode,
    string? BranchPhone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? Country,
    string? PostalCode,
    string TimeZone,
    bool BranchIsActive);

public sealed class GetPlatformBranchHandler(
    IAppDbContext db,
    IPlatformTenantService tenantService) : IRequestHandler<GetPlatformBranchQuery, PlatformBranchDetailDto>
{
    public async Task<PlatformBranchDetailDto> Handle(
        GetPlatformBranchQuery req, CancellationToken ct)
    {
        var row = await (
            from b in db.Set<Branch>().IgnoreQueryFilters().AsNoTracking()
            join c in db.Set<Company>().IgnoreQueryFilters().AsNoTracking() on b.CompanyId equals c.Id
            join t in db.Set<Tenant>().AsNoTracking() on b.TenantId equals t.Id
            where b.Id == req.BranchId
            select new { b, c, t }
        ).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Branch", req.BranchId);

        // Owner email comes from IPlatformTenantService — it already knows how to find
        // the first admin user in a tenant, and lives in Infrastructure with UserManager.
        var tenantDetail = await tenantService.GetAsync(row.t.Id, ct);

        return new PlatformBranchDetailDto(
            row.t.Id, row.t.Name, (int)row.t.BusinessType, (int)row.t.Plan, row.t.LogoUrl,
            tenantDetail.AdminEmail,
            row.c.Id, row.c.Name, row.c.LegalName,
            row.b.Id, row.b.Name, row.b.Code, row.b.PhoneNumber,
            row.b.Location.AddressLine1, row.b.Location.AddressLine2,
            row.b.Location.City, row.b.Location.State, row.b.Location.Country, row.b.Location.PostalCode,
            row.b.Location.TimeZone, row.b.IsActive);
    }
}
