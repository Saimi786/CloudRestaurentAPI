using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Domain;
using CloudRestaurent.Modules.Restaurant.Application.Printing;
using CloudRestaurent.Modules.Restaurant.Domain;
using CloudRestaurent.Modules.Sales.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.KitchenPrinting;

/// <summary>
/// Cashier-triggered "send to kitchen" — discovers every station the open order's
/// lines route to, and fires a print job at each station that has a printer
/// configured. Stations without printers, or stations whose print fails, don't
/// block the others — we collect per-station results and return them so the
/// cashier UI can flag failed prints. Failures are warnings, not errors: a
/// crashed printer should never stop the flow of orders to a working station.
/// </summary>
public sealed record FireKitchenTicketCommand(Guid TicketId)
    : IRequest<FireKitchenTicketResult>;

public sealed record FireKitchenTicketResult(
    int StationsAttempted,
    int StationsPrinted,
    IReadOnlyList<StationPrintResult> Stations);

public sealed record StationPrintResult(Guid StationId, string StationName, bool Success, string? Message);

public sealed class FireKitchenTicketValidator : AbstractValidator<FireKitchenTicketCommand>
{
    public FireKitchenTicketValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
    }
}

public sealed class FireKitchenTicketHandler(IAppDbContext db, IMediator mediator)
    : IRequestHandler<FireKitchenTicketCommand, FireKitchenTicketResult>
{
    public async Task<FireKitchenTicketResult> Handle(FireKitchenTicketCommand request, CancellationToken ct)
    {
        var ticket = await db.Set<KitchenTicket>().AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, ct)
            ?? throw new NotFoundException("KitchenTicket", request.TicketId);

        // Discover every distinct station the order's lines route to via Category → KitchenStationId.
        // Lines whose category has no station (KitchenStationId == null) are uncategorized — they
        // print at the "default" station if any single station exists at this branch, but for v1
        // we just skip them. Adding a per-branch default-station setting is a follow-up.
        var stationIds = await (
            from l in db.Set<OrderLine>().AsNoTracking()
            join p in db.Set<Product>().AsNoTracking() on l.ProductId equals p.Id
            join c in db.Set<Category>().AsNoTracking() on p.CategoryId equals c.Id
            where l.OrderId == ticket.OrderId && c.KitchenStationId != null
            select c.KitchenStationId!.Value).Distinct().ToListAsync(ct);

        if (stationIds.Count == 0)
            return new FireKitchenTicketResult(0, 0, []);

        var stations = await db.Set<KitchenStation>().AsNoTracking()
            .Where(s => stationIds.Contains(s.Id) && s.IsActive)
            .Select(s => new { s.Id, s.Name, s.PrinterIpAddress })
            .ToListAsync(ct);

        var results = new List<StationPrintResult>();
        var printed = 0;
        foreach (var station in stations)
        {
            if (string.IsNullOrEmpty(station.PrinterIpAddress))
            {
                results.Add(new StationPrintResult(station.Id, station.Name,
                    Success: false, Message: "No printer configured."));
                continue;
            }

            var print = await mediator.Send(
                new PrintKitchenTicketCommand(request.TicketId, station.Id), ct);
            results.Add(new StationPrintResult(station.Id, station.Name,
                print.Success, print.Message));
            if (print.Success) printed++;
        }

        return new FireKitchenTicketResult(stations.Count, printed, results);
    }
}
