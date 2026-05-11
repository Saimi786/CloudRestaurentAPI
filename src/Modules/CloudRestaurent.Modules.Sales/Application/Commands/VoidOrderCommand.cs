using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Sales.Application.Common;
using CloudRestaurent.Modules.Sales.Application.Dtos;
using CloudRestaurent.Modules.Restaurant.Domain;
using CloudRestaurent.Modules.Sales.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Commands;

public sealed record VoidOrderCommand(Guid OrderId) : IRequest<OrderDto>;

public sealed class VoidOrderHandler(IAppDbContext db) : IRequestHandler<VoidOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(VoidOrderCommand request, CancellationToken ct)
    {
        var order = await db.Set<Order>().FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            ?? throw new NotFoundException("Order", request.OrderId);

        try { order.Void(); }
        catch (InvalidOperationException ex) { throw new BusinessRuleException(ex.Message); }

        // Free table if assigned
        if (order.TableId is { } tid)
        {
            var table = await db.Set<RestaurantTable>().FirstOrDefaultAsync(t => t.Id == tid, ct);
            if (table?.Status == TableStatus.Occupied) table.SetStatus(TableStatus.Available);
        }

        await db.SaveChangesAsync(ct);
        return await OrderDtoBuilder.BuildAsync(db, order, ct);
    }
}
