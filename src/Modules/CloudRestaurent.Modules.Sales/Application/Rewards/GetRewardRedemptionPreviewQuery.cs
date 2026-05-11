using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Domain.Tenants;
using CloudRestaurent.Modules.Contacts.Domain;
using CloudRestaurent.Modules.Sales.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Rewards;

/// <summary>
/// Quick "what would N points be worth on this order?" preview for the POS — no writes.
/// Returns the customer's current balance, max redeemable, and the currency value the
/// cashier would get back. Mirrors UP's <c>getRewardRedeemDetails</c>.
/// </summary>
public sealed record GetRewardRedemptionPreviewQuery(Guid OrderId)
    : IRequest<RewardRedemptionPreviewDto>;

public sealed record RewardRedemptionPreviewDto(
    bool Enabled,
    string Name,
    int CustomerBalance,
    int MaxRedeemable,
    decimal RedeemValuePerPoint,
    decimal MaxRedemptionAmount,
    int? MinRedeemPoints,
    decimal? MinOrderForRedeem,
    bool OrderEligible,
    string? IneligibleReason);

public sealed class GetRewardRedemptionPreviewHandler(IAppDbContext db)
    : IRequestHandler<GetRewardRedemptionPreviewQuery, RewardRedemptionPreviewDto>
{
    public async Task<RewardRedemptionPreviewDto> Handle(
        GetRewardRedemptionPreviewQuery req, CancellationToken ct)
    {
        var order = await db.Set<Order>().AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == req.OrderId, ct)
            ?? throw new NotFoundException("Order", req.OrderId);

        var settings = await db.Set<BusinessSettings>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == order.TenantId, ct);

        if (settings is null || !settings.RewardPointsEnabled)
            return new RewardRedemptionPreviewDto(
                false, settings?.RewardPointsName ?? "Points",
                0, 0, 0, 0, null, null,
                false, "Reward points are not enabled.");

        // Customer must be attached to redeem.
        if (order.CustomerId is not { } cid)
            return new RewardRedemptionPreviewDto(
                true, settings.RewardPointsName, 0, 0,
                settings.RewardPointsRedeemValue, 0,
                settings.RewardPointsMinRedeem, settings.RewardPointsMinOrderForRedeem,
                false, "Attach a customer to use reward points.");

        var customer = await db.Set<Customer>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cid, ct);
        if (customer is null)
            return new RewardRedemptionPreviewDto(
                true, settings.RewardPointsName, 0, 0,
                settings.RewardPointsRedeemValue, 0,
                settings.RewardPointsMinRedeem, settings.RewardPointsMinOrderForRedeem,
                false, "Customer not found.");

        // Effective balance treats already-redeemed points as still-available so the UI
        // can adjust an existing redemption upward without falsely showing 0 balance.
        var effectiveBalance = customer.TotalRewardPoints + order.RewardPointsRedeemed;
        var totalBeforeRedemption = order.GrandTotalAmount + order.RewardPointsRedeemedAmount;

        var maxByCap = settings.RewardPointsMaxRedeem ?? int.MaxValue;
        var maxRedeemable = Math.Min(effectiveBalance, maxByCap);

        var orderEligible = totalBeforeRedemption >= settings.RewardPointsMinOrderForRedeem;
        var reason = orderEligible
            ? null
            : $"Order must be at least {settings.RewardPointsMinOrderForRedeem:0.00} to redeem points.";

        var maxAmount = maxRedeemable * settings.RewardPointsRedeemValue;
        if (maxAmount > totalBeforeRedemption) maxAmount = totalBeforeRedemption;

        return new RewardRedemptionPreviewDto(
            true, settings.RewardPointsName,
            effectiveBalance, maxRedeemable,
            settings.RewardPointsRedeemValue,
            decimal.Round(maxAmount, 2, MidpointRounding.AwayFromZero),
            settings.RewardPointsMinRedeem,
            settings.RewardPointsMinOrderForRedeem,
            orderEligible, reason);
    }
}
