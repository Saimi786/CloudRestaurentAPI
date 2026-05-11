using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Restaurant.Application.KitchenStations.Dtos;
using CloudRestaurent.Modules.Restaurant.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Restaurant.Application.KitchenStations.Queries;

public sealed record GetKitchenStationsQuery(
    Guid? BranchId = null,
    bool IncludeInactive = false) : IRequest<IReadOnlyList<KitchenStationDto>>;

public sealed class GetKitchenStationsHandler(IAppDbContext db)
    : IRequestHandler<GetKitchenStationsQuery, IReadOnlyList<KitchenStationDto>>
{
    public async Task<IReadOnlyList<KitchenStationDto>> Handle(GetKitchenStationsQuery request, CancellationToken ct)
    {
        var query = db.Set<KitchenStation>().AsNoTracking();
        if (request.BranchId is { } b) query = query.Where(s => s.BranchId == b);
        if (!request.IncludeInactive) query = query.Where(s => s.IsActive);

        return await (
            from s in query
            join br in db.Set<Branch>().AsNoTracking() on s.BranchId equals br.Id
            orderby s.DisplayOrder, s.Name
            select new KitchenStationDto(
                s.Id, s.BranchId, br.Name,
                s.Name, s.DisplayOrder, s.Description, s.IsActive,
                s.PrinterIpAddress, s.PrinterPort))
            .ToListAsync(ct);
    }
}
