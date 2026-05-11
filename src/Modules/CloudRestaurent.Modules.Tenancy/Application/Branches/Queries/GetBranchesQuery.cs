using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Tenancy.Application.Branches.Dtos;
using CloudRestaurent.Domain.Companies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tenancy.Application.Branches.Queries;

public sealed record GetBranchesQuery(Guid? CompanyId = null, bool IncludeInactive = false)
    : IRequest<IReadOnlyList<BranchDto>>;

public sealed class GetBranchesHandler(IAppDbContext db)
    : IRequestHandler<GetBranchesQuery, IReadOnlyList<BranchDto>>
{
    public async Task<IReadOnlyList<BranchDto>> Handle(GetBranchesQuery request, CancellationToken ct)
    {
        var query = db.Set<Branch>().AsNoTracking();
        if (request.CompanyId is { } companyId)
            query = query.Where(b => b.CompanyId == companyId);
        if (!request.IncludeInactive)
            query = query.Where(b => b.IsActive);

        return await query
            .OrderBy(b => b.Name)
            .Select(b => new BranchDto(
                b.Id, b.CompanyId, b.Name, b.Code, b.PhoneNumber,
                new LocationDto(
                    b.Location.AddressLine1, b.Location.AddressLine2,
                    b.Location.City, b.Location.State, b.Location.Country,
                    b.Location.PostalCode, b.Location.Latitude, b.Location.Longitude,
                    b.Location.TimeZone),
                b.IsActive,
                (int)b.ReceiptTemplate,
                b.ReceiptFooterText))
            .ToListAsync(ct);
    }
}
