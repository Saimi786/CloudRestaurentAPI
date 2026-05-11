using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Domain.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tenancy.Application.Platform;

/// <summary>
/// Flat list of every active branch across every tenant — used by the top-bar
/// location picker. SuperAdmin only; bypasses the per-tenant query filter so
/// the dropdown can show "Demo Restaurant — Main Branch" alongside
/// "Acme Foods — Karachi Outlet" etc.
/// </summary>
public sealed record GetAllPlatformBranchesQuery : IRequest<IReadOnlyList<PlatformBranchPickDto>>;

public sealed record PlatformBranchPickDto(
    Guid BranchId,
    Guid TenantId,
    string TenantName,
    string BranchName,
    string BranchCode,
    string? City,
    string? Country,
    bool IsActive);

public sealed class GetAllPlatformBranchesHandler(IAppDbContext db)
    : IRequestHandler<GetAllPlatformBranchesQuery, IReadOnlyList<PlatformBranchPickDto>>
{
    public async Task<IReadOnlyList<PlatformBranchPickDto>> Handle(
        GetAllPlatformBranchesQuery _, CancellationToken ct) =>
        await (
            from b in db.Set<Branch>().IgnoreQueryFilters().AsNoTracking()
            join t in db.Set<Tenant>().AsNoTracking() on b.TenantId equals t.Id
            where b.IsActive
            orderby t.Name, b.Name
            select new PlatformBranchPickDto(
                b.Id, t.Id, t.Name,
                b.Name, b.Code,
                b.Location.City, b.Location.Country,
                b.IsActive)
        ).ToListAsync(ct);
}
