using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Sales.Application.Kitchen.Dtos;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Catalog.Domain;
using CloudRestaurent.Modules.Restaurant.Domain;
using CloudRestaurent.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Kitchen.Common;

internal static class KitchenTicketDtoBuilder
{
    public static async Task<KitchenTicketDto> BuildAsync(
        IAppDbContext db, KitchenTicket ticket, CancellationToken ct)
    {
        var order = await db.Set<Order>().AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == ticket.OrderId, ct)
            ?? throw new NotFoundException("Order", ticket.OrderId);

        var branch = await db.Set<Branch>().AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == ticket.BranchId, ct)
            ?? throw new NotFoundException("Branch", ticket.BranchId);

        string? tableCode = null;
        if (order.TableId is { } tid)
            tableCode = await db.Set<RestaurantTable>().AsNoTracking()
                .Where(t => t.Id == tid).Select(t => t.Code).FirstOrDefaultAsync(ct);

        // Pull each line's product, then product's category, then category's kitchen station
        // — all so the kitchen UI can route lines to the right station screen.
        var lines = await (
            from l in db.Set<OrderLine>().AsNoTracking()
            where l.OrderId == order.Id
            join p in db.Set<Product>().AsNoTracking() on l.ProductId equals p.Id
            join c in db.Set<Category>().AsNoTracking() on p.CategoryId equals c.Id
            select new {
                l.Id, l.ProductName, l.Quantity, l.Notes,
                StationId = c.KitchenStationId
            })
            .ToListAsync(ct);

        var stationIds = lines.Where(l => l.StationId.HasValue)
            .Select(l => l.StationId!.Value).Distinct().ToList();
        var stationNames = stationIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await db.Set<KitchenStation>().AsNoTracking()
                .Where(s => stationIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var lineIds = lines.Select(l => l.Id).ToList();
        var modifiers = (await db.Set<OrderLineModifier>().AsNoTracking()
            .Where(m => lineIds.Contains(m.OrderLineId))
            .Select(m => new { m.OrderLineId, m.Name })
            .ToListAsync(ct))
            .GroupBy(m => m.OrderLineId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var lineDtos = lines.Select(l => new KitchenTicketLineDto(
            l.ProductName, l.Quantity, l.Notes,
            l.StationId,
            l.StationId is { } sid ? stationNames.GetValueOrDefault(sid) : null,
            modifiers.GetValueOrDefault(l.Id, [])
        )).ToList();

        var minutesOpen = (int)Math.Round((DateTimeOffset.UtcNow - ticket.OpenedAt).TotalMinutes);
        var involved = stationIds;
        var bumped = ticket.GetBumpedStations();

        return new KitchenTicketDto(
            ticket.Id, ticket.OrderId, order.OrderNumber,
            ticket.BranchId, branch.Name, tableCode,
            order.Type, order.Type.ToString(),
            ticket.Status, ticket.Status.ToString(),
            ticket.OpenedAt, ticket.ReadyAt, ticket.ServedAt,
            minutesOpen, involved, bumped, lineDtos);
    }
}
