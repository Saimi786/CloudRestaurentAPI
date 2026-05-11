using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Domain.Companies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tenancy.Application.Platform;

/// <summary>
/// Cross-tenant branch listing for the SuperAdmin "Manage Business" page.
/// Bypasses the per-tenant query filter — every branch under <paramref name="TenantId"/>
/// is returned regardless of who's logged in. Authorization is the controller's job.
/// </summary>
public sealed record GetTenantBranchesQuery(Guid TenantId)
    : IRequest<IReadOnlyList<PlatformBranchDto>>;

public sealed record PlatformBranchDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string Name,
    string Code,
    string? PhoneNumber,
    string? City,
    string? State,
    string? Country,
    string? AddressLine1,
    string TimeZone,
    bool IsActive);

public sealed class GetTenantBranchesHandler(IAppDbContext db)
    : IRequestHandler<GetTenantBranchesQuery, IReadOnlyList<PlatformBranchDto>>
{
    public async Task<IReadOnlyList<PlatformBranchDto>> Handle(
        GetTenantBranchesQuery req, CancellationToken ct)
    {
        // Existence check (returns 404 rather than an empty list when the tenant id is bad).
        var tenantExists = await db.Set<CloudRestaurent.Domain.Tenants.Tenant>()
            .AnyAsync(t => t.Id == req.TenantId, ct);
        if (!tenantExists) throw new NotFoundException("Tenant", req.TenantId);

        var rows = await (
            from b in db.Set<Branch>().IgnoreQueryFilters().AsNoTracking()
            join c in db.Set<Company>().IgnoreQueryFilters().AsNoTracking() on b.CompanyId equals c.Id
            where b.TenantId == req.TenantId
            orderby c.Name, b.Name
            select new PlatformBranchDto(
                b.Id, c.Id, c.Name,
                b.Name, b.Code, b.PhoneNumber,
                b.Location.City, b.Location.State, b.Location.Country, b.Location.AddressLine1,
                b.Location.TimeZone, b.IsActive)
        ).ToListAsync(ct);

        return rows;
    }
}
