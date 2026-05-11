using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Sales.Application.Common;
using CloudRestaurent.Modules.Sales.Application.Dtos;
using CloudRestaurent.Modules.Sales.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Commands;

public sealed record SetOrderDiscountCommand(Guid OrderId, decimal DiscountAmount) : IRequest<OrderDto>;

public sealed class SetOrderDiscountValidator : AbstractValidator<SetOrderDiscountCommand>
{
    public SetOrderDiscountValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0);
    }
}

public sealed class SetOrderDiscountHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<SetOrderDiscountCommand, OrderDto>
{
    public async Task<OrderDto> Handle(SetOrderDiscountCommand request, CancellationToken ct)
    {
        var order = await db.Set<Order>().FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            ?? throw new NotFoundException("Order", request.OrderId);

        if (order.Status != OrderStatus.Open)
            throw new BusinessRuleException("Cannot change discount on a non-open order.");

        currentUser.EnsureCanAccess(order.BranchId);

        // Refuse a discount that would negate the entire order — typo guard.
        var preDiscountTotal = order.SubtotalAmount + order.TaxAmount;
        if (request.DiscountAmount > preDiscountTotal)
            throw new BusinessRuleException(
                $"Discount {request.DiscountAmount:0.00} exceeds order pre-discount total {preDiscountTotal:0.00}.");

        // Cap the discount at the user's max % if they have one. We measure against the
        // pre-discount total (subtotal + tax) so the cashier can't game it by sending the
        // discount in smaller batches.
        if (currentUser.MaxDiscountPercent is { } cap && preDiscountTotal > 0)
        {
            var requestedPercent = request.DiscountAmount / preDiscountTotal * 100m;
            if (requestedPercent > cap)
                throw new ForbiddenException(
                    $"Discount {requestedPercent:0.##}% exceeds your maximum of {cap:0.##}%. " +
                    "Ask a manager to apply this discount.");
        }

        order.SetDiscount(request.DiscountAmount);
        await db.SaveChangesAsync(ct);

        return await OrderDtoBuilder.BuildAsync(db, order, ct);
    }
}
