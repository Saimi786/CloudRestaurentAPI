using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Sales.Application.Common;
using CloudRestaurent.Modules.Sales.Application.Dtos;
using CloudRestaurent.Modules.Sales.Application.Promotions;
using CloudRestaurent.Domain.Common;
using CloudRestaurent.Modules.Sales.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Commands;

public sealed record OverrideLinePriceCommand(Guid OrderId, Guid LineId, decimal UnitPrice)
    : IRequest<OrderDto>;

public sealed class OverrideLinePriceValidator : AbstractValidator<OverrideLinePriceCommand>
{
    public OverrideLinePriceValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
    }
}

public sealed class OverrideLinePriceHandler(
    IAppDbContext db, ICurrentUser currentUser, PromotionRecomputer promotions)
    : IRequestHandler<OverrideLinePriceCommand, OrderDto>
{
    public async Task<OrderDto> Handle(OverrideLinePriceCommand request, CancellationToken ct)
    {
        var order = await db.Set<Order>().FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            ?? throw new NotFoundException("Order", request.OrderId);

        if (order.Status != OrderStatus.Open)
            throw new BusinessRuleException("Cannot override prices on a non-open order.");

        currentUser.EnsureCanAccess(order.BranchId);

        var line = await db.Set<OrderLine>().FirstOrDefaultAsync(
            l => l.Id == request.LineId && l.OrderId == order.Id, ct)
            ?? throw new NotFoundException("OrderLine", request.LineId);

        line.OverrideUnitPrice(new Money(request.UnitPrice, line.UnitPrice.Currency));

        // Re-snapshot the line totals so receipt math stays consistent. Modifier contributions
        // were already baked into LineSubtotal; we need to back them out and re-add them.
        var modifierAmounts = await db.Set<OrderLineModifier>().AsNoTracking()
            .Where(m => m.OrderLineId == line.Id)
            .Select(m => m.PriceAdjustment.Amount)
            .ToListAsync(ct);
        var modifiersTotal = modifierAmounts.Sum();
        line.SnapshotTotals(modifiersTotal, line.TaxRateId, line.TaxRatePercentage);

        await db.SaveChangesAsync(ct);

        var allLines = await db.Set<OrderLine>().AsNoTracking()
            .Where(l => l.OrderId == order.Id)
            .ToListAsync(ct);
        order.RecomputeTotals(allLines);
        await promotions.RecomputeAsync(order, DateTime.Now, ct);
        await db.SaveChangesAsync(ct);

        return await OrderDtoBuilder.BuildAsync(db, order, ct);
    }
}
