using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Sales.Application.Common;
using CloudRestaurent.Modules.Sales.Application.Dtos;
using CloudRestaurent.Modules.Sales.Application.Promotions;
using CloudRestaurent.Modules.Sales.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Commands;

public sealed record RemoveOrderLineCommand(Guid OrderId, Guid LineId) : IRequest<OrderDto>;

public sealed class RemoveOrderLineHandler(IAppDbContext db, PromotionRecomputer promotions)
    : IRequestHandler<RemoveOrderLineCommand, OrderDto>
{
    public async Task<OrderDto> Handle(RemoveOrderLineCommand request, CancellationToken ct)
    {
        var order = await db.Set<Order>().FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            ?? throw new NotFoundException("Order", request.OrderId);
        if (order.Status != OrderStatus.Open)
            throw new BusinessRuleException("Cannot modify a non-open order.");

        var line = await db.Set<OrderLine>()
            .FirstOrDefaultAsync(l => l.Id == request.LineId && l.OrderId == request.OrderId, ct)
            ?? throw new NotFoundException("OrderLine", request.LineId);

        // Cascade delete will pull child modifiers along.
        db.Set<OrderLine>().Remove(line);
        await db.SaveChangesAsync(ct);

        // Recompute order totals from the remaining lines.
        var remaining = await db.Set<OrderLine>().AsNoTracking()
            .Where(l => l.OrderId == order.Id)
            .ToListAsync(ct);
        order.RecomputeTotals(remaining);

        await promotions.RecomputeAsync(order, DateTime.Now, ct);
        await db.SaveChangesAsync(ct);

        return await OrderDtoBuilder.BuildAsync(db, order, ct);
    }
}
