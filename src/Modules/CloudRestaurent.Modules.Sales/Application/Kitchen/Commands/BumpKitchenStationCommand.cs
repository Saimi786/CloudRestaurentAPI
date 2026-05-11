using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Sales.Application.Kitchen.Common;
using CloudRestaurent.Modules.Sales.Application.Kitchen.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;
using CloudRestaurent.Modules.Restaurant.Domain;
using CloudRestaurent.Modules.Sales.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Kitchen.Commands;

/// <summary>
/// A station marks its portion of a ticket "ready". When all stations involved in
/// the ticket have bumped, the ticket auto-advances to Ready so expediter can plate up.
/// </summary>
public sealed record BumpKitchenStationCommand(Guid TicketId, Guid StationId, bool Unbump = false)
    : IRequest<KitchenTicketDto>;

public sealed class BumpKitchenStationHandler(IAppDbContext db, IKitchenNotifier kitchen)
    : IRequestHandler<BumpKitchenStationCommand, KitchenTicketDto>
{
    public async Task<KitchenTicketDto> Handle(BumpKitchenStationCommand request, CancellationToken ct)
    {
        var ticket = await db.Set<KitchenTicket>().FirstOrDefaultAsync(t => t.Id == request.TicketId, ct)
            ?? throw new NotFoundException("KitchenTicket", request.TicketId);

        if (ticket.Status == KitchenTicketStatus.Served)
            throw new BusinessRuleException("Ticket is already served.");

        // Verify the station belongs to this branch.
        var station = await db.Set<KitchenStation>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.StationId, ct)
            ?? throw new NotFoundException("KitchenStation", request.StationId);
        if (station.BranchId != ticket.BranchId)
            throw new BusinessRuleException("Station belongs to a different branch.");

        // Compute which stations are actually involved in this ticket
        // (distinct kitchen-station IDs from the categories of each line's product).
        var involved = await (
            from l in db.Set<OrderLine>().AsNoTracking()
            where l.OrderId == ticket.OrderId
            join p in db.Set<Product>().AsNoTracking() on l.ProductId equals p.Id
            join c in db.Set<Category>().AsNoTracking() on p.CategoryId equals c.Id
            where c.KitchenStationId != null
            select c.KitchenStationId!.Value)
            .Distinct().ToListAsync(ct);

        if (!involved.Contains(request.StationId))
            throw new BusinessRuleException(
                "This station has nothing on this ticket — only stations with at least one line can bump it.");

        if (request.Unbump) ticket.UnbumpStation(request.StationId);
        else ticket.BumpStation(request.StationId);

        // Auto-advance Pending → Preparing on first bump (something is being cooked).
        if (ticket.Status == KitchenTicketStatus.Pending && !request.Unbump)
            ticket.Advance(KitchenTicketStatus.Preparing);

        // Auto-advance to Ready when every involved station has bumped.
        if (ticket.Status == KitchenTicketStatus.Preparing
            && ticket.AreAllStationsBumped(involved))
            ticket.Advance(KitchenTicketStatus.Ready);

        await db.SaveChangesAsync(ct);
        await kitchen.TicketChangedAsync(ticket.TenantId, ticket.BranchId, ticket.Id, ct);

        return await KitchenTicketDtoBuilder.BuildAsync(db, ticket, ct);
    }
}
