using CloudRestaurent.Domain.Tenants;

namespace CloudRestaurent.Modules.Sales.Application.Rewards;

/// <summary>
/// Pure functions for reward-point math. Mirrors UltimatePOS's
/// <c>TransactionUtil::calculateRewardPoints</c> / <c>getRewardRedeemDetails</c> exactly:
///
///   <c>points = floor(orderTotal / amount_per_point)</c>, capped at <c>max_per_order</c>,
///   gated by <c>min_order_for_earn</c>.
///
///   redemption value = <c>pointsRequested * redeem_value</c>, gated by
///   <c>min_redeem</c>, <c>max_redeem</c>, customer balance, and <c>min_order_for_redeem</c>.
/// </summary>
public static class RewardPointsCalculator
{
    public static int CalculateEarned(BusinessSettings settings, decimal orderGrandTotal)
    {
        if (!settings.RewardPointsEnabled) return 0;
        if (orderGrandTotal < settings.RewardPointsMinOrderForEarn) return 0;
        if (settings.RewardPointsAmountPerPoint <= 0) return 0;

        var raw = (int)Math.Floor(orderGrandTotal / settings.RewardPointsAmountPerPoint);
        if (settings.RewardPointsMaxPerOrder is { } cap && raw > cap) raw = cap;
        return raw < 0 ? 0 : raw;
    }

    /// <summary>
    /// Validate a redemption request. Returns (true, currencyValue) when the request is
    /// allowed; otherwise (false, reason).
    /// </summary>
    public static (bool Ok, decimal Amount, string? Reason) ValidateRedemption(
        BusinessSettings settings,
        int requestedPoints,
        int customerBalance,
        decimal orderGrandTotalBeforeRedemption)
    {
        if (!settings.RewardPointsEnabled)
            return (false, 0m, "Reward points are not enabled.");
        if (requestedPoints <= 0)
            return (false, 0m, "Redeem at least 1 point.");
        if (requestedPoints > customerBalance)
            return (false, 0m, $"Customer only has {customerBalance} points.");
        if (settings.RewardPointsMinRedeem is { } min && requestedPoints < min)
            return (false, 0m, $"Minimum {min} points required to redeem.");
        if (settings.RewardPointsMaxRedeem is { } max && requestedPoints > max)
            return (false, 0m, $"Maximum {max} points per redemption.");
        if (orderGrandTotalBeforeRedemption < settings.RewardPointsMinOrderForRedeem)
            return (false, 0m,
                $"Order must be at least {settings.RewardPointsMinOrderForRedeem:0.00} to redeem points.");

        var amount = requestedPoints * settings.RewardPointsRedeemValue;

        // Don't let the redemption pull the order negative — clamp.
        if (amount > orderGrandTotalBeforeRedemption)
            amount = orderGrandTotalBeforeRedemption;

        return (true, decimal.Round(amount, 2, MidpointRounding.AwayFromZero), null);
    }
}
