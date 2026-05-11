using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Catalog.Domain;
using CloudRestaurent.Modules.Sales.Application.Kitchen.Common;
using CloudRestaurent.Modules.Sales.Application.Kitchen.Dtos;
using CloudRestaurent.Modules.Restaurant.Domain;
using CloudRestaurent.Modules.Sales.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Kitchen.Queries;

public sealed record GetKitchenTicketsQuery(
    Guid? BranchId = null,
    Guid? StationId = null,
    bool IncludeServed = false) : IRequest<IReadOnlyList<KitchenTicketDto>>;

public sealed class GetKitchenTicketsHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetKitchenTicketsQuery, IReadOnlyList<KitchenTicketDto>>
{
    public async Task<IReadOnlyList<KitchenTicketDto>> Handle(GetKitchenTicketsQuery request, CancellationToken ct)
    {
        var query = db.Set<KitchenTicket>().AsNoTracking();
        if (request.BranchId is { } b)
        {
            currentUser.EnsureCanAccess(b);
            query = query.Where(t => t.BranchId == b);
        }
        else if (!currentUser.CanAccessAllBranches)
        {
            var allowed = currentUser.BranchIds;
            query = query.Where(t => allowed.Contains(t.BranchId));
        }
        if (!request.IncludeServed) query = query.Where(t => t.Status != KitchenTicketStatus.Served);

        // If filtering by station, only return tickets that have at least one line whose
        // product's category routes to that station. Saves the kitchen-grill-station screen
        // from showing tickets that have no grill items on them.
        if (request.StationId is { } stationId)
        {
            query = query.Where(t =>
                db.Set<OrderLine>()
                    .Where(l => l.OrderId == t.OrderId)
                    .Join(db.Set<Product>(), l => l.ProductId, p => p.Id, (l, p) => p)
                    .Join(db.Set<Category>(), p => p.CategoryId, c => c.Id, (p, c) => c)
                    .Any(c => c.KitchenStationId == stationId));
        }

        var tickets = await query
            .OrderBy(t => t.OpenedAt)
            .ToListAsync(ct);

        var dtos = new List<KitchenTicketDto>(tickets.Count);
        foreach (var t in tickets)
            dtos.Add(await KitchenTicketDtoBuilder.BuildAsync(db, t, ct));

        // When a station is requested, also filter the per-ticket lines down to lines for that station.
        if (request.StationId is { } sid)
        {
            dtos = dtos.Select(d => d with {
                Lines = d.Lines.Where(l => l.KitchenStationId == sid).ToList()
            }).ToList();
        }

        return dtos;
    }
}
