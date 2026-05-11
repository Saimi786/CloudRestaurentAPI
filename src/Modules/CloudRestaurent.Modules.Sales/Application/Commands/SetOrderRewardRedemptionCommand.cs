using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Domain.Tenants;
using CloudRestaurent.Modules.Contacts.Domain;
using CloudRestaurent.Modules.Sales.Application.Common;
using CloudRestaurent.Modules.Sales.Application.Dtos;
using CloudRestaurent.Modules.Sales.Application.Rewards;
using CloudRestaurent.Modules.Sales.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Commands;

/// <summary>
/// Apply (or clear) a reward-points redemption against an open order. Mirrors UP's
/// flow: cashier picks a customer, sees their balance, optionally redeems N points
/// for an automatically-computed currency discount.
///
/// Passing <c>Points = 0</c> clears any existing redemption.
/// </summary>
public sealed record SetOrderRewardRedemptionCommand(Guid OrderId, int Points) : IRequest<OrderDto>;

public sealed class SetOrderRewardRedemptionValidator : AbstractValidator<SetOrderRewardRedemptionCommand>
{
    public SetOrderRewardRedemptionValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Points).GreaterThanOrEqualTo(0);
    }
}

public sealed class SetOrderRewardRedemptionHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<SetOrderRewardRedemptionCommand, OrderDto>
{
    public async Task<OrderDto> Handle(SetOrderRewardRedemptionCommand request, CancellationToken ct)
    {
        var order = await db.Set<Order>().FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            ?? throw new NotFoundException("Order", request.OrderId);

        if (order.Status != OrderStatus.Open)
            throw new BusinessRuleException("Cannot redeem points on a non-open order.");

        currentUser.EnsureCanAccess(order.BranchId);

        if (order.CustomerId is not { } customerId)
            throw new BusinessRuleException("Attach a customer before redeeming reward points.");

        var customer = await db.Set<Customer>().FirstOrDefaultAsync(c => c.Id == customerId && c.IsActive, ct)
            ?? throw new NotFoundException("Customer", customerId);

        // Clearing a redemption: refund the previously-redeemed points to the customer.
        if (request.Points == 0)
        {
            if (order.RewardPointsRedeemed > 0)
                customer.ReverseRedemption(order.RewardPointsRedeemed);
            order.SetRewardRedemption(0, 0m);
            await db.SaveChangesAsync(ct);
            return await OrderDtoBuilder.BuildAsync(db, order, ct);
        }

        var settings = await db.Set<BusinessSettings>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == order.TenantId, ct)
            ?? throw new BusinessRuleException("Business settings are not configured for this tenant.");

        // Effective balance for validation = current + any points already redeemed on this
        // order (so editing an existing redemption doesn't fail just because the points have
        // moved to the order side).
        var effectiveBalance = customer.TotalRewardPoints + order.RewardPointsRedeemed;

        // Compute the order total BEFORE redemption so the redeem-value cap is fair.
        var totalBeforeRedemption = order.GrandTotalAmount + order.RewardPointsRedeemedAmount;

        var (ok, amount, reason) = RewardPointsCalculator.ValidateRedemption(
            settings, request.Points, effectiveBalance, totalBeforeRedemption);
        if (!ok) throw new BusinessRuleException(reason ?? "Redemption rejected.");

        // First reverse any prior redemption on this order, then apply the new one.
        if (order.RewardPointsRedeemed > 0)
            customer.ReverseRedemption(order.RewardPointsRedeemed);
        customer.RedeemPoints(request.Points);

        order.SetRewardRedemption(request.Points, amount);
        await db.SaveChangesAsync(ct);

        return await OrderDtoBuilder.BuildAsync(db, order, ct);
    }
}
