using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Restaurant.Application.Printing;
using CloudRestaurent.Modules.Restaurant.Domain;
using CloudRestaurent.Modules.Sales.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CloudRestaurent.Modules.Catalog.Domain;
using CloudRestaurent.Modules.Contacts.Domain;

namespace CloudRestaurent.Modules.Sales.Application.KitchenPrinting;

/// <summary>
/// Send a kitchen ticket to a station's network printer. KDS clients call this
/// when a ticket is created (or admins fire it manually for re-prints). If the
/// station has no printer configured we return a soft-fail — POS shouldn't fail
/// just because a back-office screen wasn't wired to a printer.
/// </summary>
public sealed record PrintKitchenTicketCommand(Guid TicketId, Guid StationId)
    : IRequest<PrintResult>;

public sealed class PrintKitchenTicketValidator : AbstractValidator<PrintKitchenTicketCommand>
{
    public PrintKitchenTicketValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.StationId).NotEmpty();
    }
}

public sealed class PrintKitchenTicketHandler(
    IAppDbContext db,
    IPrinterAdapter printer)
    : IRequestHandler<PrintKitchenTicketCommand, PrintResult>
{
    public async Task<PrintResult> Handle(PrintKitchenTicketCommand request, CancellationToken ct)
    {
        var station = await db.Set<KitchenStation>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.StationId, ct)
            ?? throw new NotFoundException("KitchenStation", request.StationId);

        if (string.IsNullOrEmpty(station.PrinterIpAddress))
            return new PrintResult(false, "Station has no printer configured.");

        var ticket = await db.Set<KitchenTicket>().AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, ct)
            ?? throw new NotFoundException("KitchenTicket", request.TicketId);

        var order = await db.Set<Order>().AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == ticket.OrderId, ct)
            ?? throw new NotFoundException("Order", ticket.OrderId);

        // Pull all lines + station-relevant ones. We filter by station via Category.KitchenStationId
        // so each station only sees the items it's responsible for cooking.
        var lines = await (
            from l in db.Set<OrderLine>().AsNoTracking()
            join p in db.Set<Product>().AsNoTracking() on l.ProductId equals p.Id
            join c in db.Set<Category>().AsNoTracking() on p.CategoryId equals c.Id
            where l.OrderId == order.Id
               && (c.KitchenStationId == request.StationId || c.KitchenStationId == null)
            orderby l.Id
            select new { l.Id, l.Quantity, l.ProductName, l.Notes }).ToListAsync(ct);

        if (lines.Count == 0)
            return new PrintResult(false, "No lines for this station on this order.");

        var lineIds = lines.Select(l => l.Id).ToHashSet();
        var modByLine = await db.Set<OrderLineModifier>().AsNoTracking()
            .Where(m => lineIds.Contains(m.OrderLineId))
            .GroupBy(m => m.OrderLineId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(m => m.Name).ToList(), ct);

        string? customerName = null;
        if (order.CustomerId.HasValue)
        {
            customerName = await db.Set<Customer>().AsNoTracking()
                .Where(c => c.Id == order.CustomerId.Value)
                .Select(c => c.FullName).FirstOrDefaultAsync(ct);
        }

        string? tableCode = null;
        if (order.TableId.HasValue)
        {
            tableCode = await db.Set<RestaurantTable>().AsNoTracking()
                .Where(t => t.Id == order.TableId.Value)
                .Select(t => t.Code).FirstOrDefaultAsync(ct);
        }

        var payload = new KitchenPrintPayload(
            OrderNumber: order.OrderNumber ?? order.Id.ToString()[..8],
            StationName: station.Name,
            TableCode: tableCode,
            CustomerName: customerName,
            OpenedAt: order.OpenedAt,
            Lines: lines.Select(l => new KitchenPrintLine(
                Quantity: (int)l.Quantity,
                ProductName: l.ProductName,
                Modifiers: modByLine.GetValueOrDefault(l.Id) ?? new List<string>(),
                Notes: l.Notes)).ToList());

        return await printer.PrintTicketAsync(
            station.PrinterIpAddress!,
            station.PrinterPort ?? 9100,
            payload,
            ct);
    }
}
