using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Sales.Application.Kitchen.Common;
using CloudRestaurent.Modules.Sales.Application.Kitchen.Dtos;
using CloudRestaurent.Modules.Restaurant.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Kitchen.Commands;

public sealed record AdvanceKitchenTicketCommand(Guid Id, KitchenTicketStatus Next)
    : IRequest<KitchenTicketDto>;

public sealed class AdvanceKitchenTicketHandler(IAppDbContext db, IKitchenNotifier notifier)
    : IRequestHandler<AdvanceKitchenTicketCommand, KitchenTicketDto>
{
    public async Task<KitchenTicketDto> Handle(AdvanceKitchenTicketCommand request, CancellationToken ct)
    {
        var ticket = await db.Set<KitchenTicket>().FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            ?? throw new NotFoundException("KitchenTicket", request.Id);

        try { ticket.Advance(request.Next); }
        catch (InvalidOperationException ex) { throw new BusinessRuleException(ex.Message); }

        await db.SaveChangesAsync(ct);
        await notifier.TicketChangedAsync(ticket.TenantId, ticket.BranchId, ticket.Id, ct);

        return await KitchenTicketDtoBuilder.BuildAsync(db, ticket, ct);
    }
}
