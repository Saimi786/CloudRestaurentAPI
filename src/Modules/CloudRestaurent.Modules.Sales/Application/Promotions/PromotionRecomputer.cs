using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Pricing.Domain;
using CloudRestaurent.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Promotions;

/// <summary>
/// Cart-time evaluator for Mix &amp; Match groups. Walks each active group, checks
/// date / day / time windows, and computes a discount per group based on its type.
/// Each group is evaluated independently — additive, not "best of."
/// Lives in Sales because evaluating needs both <see cref="OrderLine"/> (Sales) and
/// <see cref="MixMatchGroup"/> (Pricing); Sales already depends on Pricing.
/// </summary>
public sealed class PromotionRecomputer(IAppDbContext db)
{
    public async Task<decimal> RecomputeAsync(Order order, DateTime now, CancellationToken ct)
    {
        // Drop existing applied promotions for this order — we recompute from scratch.
        await db.Set<OrderPromotion>()
            .Where(p => p.OrderId == order.Id)
            .ExecuteDeleteAsync(ct);

        var lines = await db.Set<OrderLine>().AsNoTracking()
            .Where(l => l.OrderId == order.Id)
            .Select(l => new { l.ProductId, l.Quantity, l.LineSubtotal })
            .ToListAsync(ct);
        if (lines.Count == 0)
        {
            order.SetPromotionDiscount(0);
            return 0;
        }

        var groups = await db.Set<MixMatchGroup>().AsNoTracking()
            .Where(g => g.IsActive)
            .ToListAsync(ct);
        var inWindow = groups.Where(g => GroupAppliesAt(g, now)).ToList();
        if (inWindow.Count == 0)
        {
            order.SetPromotionDiscount(0);
            return 0;
        }

        var groupIds = inWindow.Select(g => g.Id).ToList();
        var memberships = await db.Set<MixMatchProduct>().AsNoTracking()
            .Where(p => groupIds.Contains(p.MixMatchGroupId))
            .Select(p => new { p.MixMatchGroupId, p.ProductId })
            .ToListAsync(ct);
        var productsByGroup = memberships
            .GroupBy(m => m.MixMatchGroupId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ProductId).ToHashSet());

        // First pass: compute the discount that *would* apply for every qualifying group.
        var candidates = new List<(MixMatchGroup grp, decimal discount, string description)>();
        foreach (var grp in inWindow)
        {
            if (!productsByGroup.TryGetValue(grp.Id, out var memberSet) || memberSet.Count == 0)
                continue;

            var qualifying = lines.Where(l => memberSet.Contains(l.ProductId)).ToList();
            var totalQty = (int)qualifying.Sum(l => Math.Floor(l.Quantity));
            if (totalQty < grp.Quantity) continue;

            var bundles = totalQty / grp.Quantity;
            var qualifyingSubtotal = qualifying.Sum(l => l.LineSubtotal);

            decimal discount = grp.Type switch
            {
                MixMatchType.DiscountAmount => bundles * grp.DiscountValue,
                MixMatchType.PercentDiscount => Math.Round(qualifyingSubtotal * grp.DiscountValue / 100m, 2),
                MixMatchType.FixedPrice => ApplyFixedPrice(qualifyingSubtotal, totalQty, grp.Quantity, grp.DiscountValue, bundles),
                _ => 0m
            };

            if (discount > qualifyingSubtotal) discount = qualifyingSubtotal;
            if (discount <= 0) continue;

            var description = grp.Type == MixMatchType.PercentDiscount
                ? $"{grp.Quantity} for {grp.DiscountValue}% off"
                : grp.Type == MixMatchType.FixedPrice
                    ? $"{grp.Quantity} for {grp.DiscountValue:0.00}"
                    : $"{grp.Quantity} for {grp.DiscountValue:0.00} off";

            candidates.Add((grp, Math.Round(discount, 2), description));
        }

        // Second pass: apply best-of for non-stackable, additive for stackable.
        // Among non-stackable candidates we pick exactly one — highest discount, ties
        // broken by Priority (higher wins), then by group name for stable ordering.
        var nonStackable = candidates.Where(c => !c.grp.Stackable)
            .OrderByDescending(c => c.discount)
            .ThenByDescending(c => c.grp.Priority)
            .ThenBy(c => c.grp.Name)
            .ToList();
        var stackable = candidates.Where(c => c.grp.Stackable).ToList();

        var winners = new List<(MixMatchGroup grp, decimal discount, string description)>(stackable);
        if (nonStackable.Count > 0) winners.Add(nonStackable[0]);

        decimal totalDiscount = 0;
        foreach (var (grp, discount, description) in winners)
        {
            db.Set<OrderPromotion>().Add(new OrderPromotion(
                Guid.NewGuid(), order.Id, grp.Id, grp.Name, description, discount));
            totalDiscount += discount;
        }

        order.SetPromotionDiscount(totalDiscount);
        return totalDiscount;
    }

    private static decimal ApplyFixedPrice(decimal subtotal, int totalQty, int threshold, decimal fixedPrice, int bundles)
    {
        var avgPerUnit = totalQty == 0 ? 0 : subtotal / totalQty;
        var bundleSubtotal = avgPerUnit * threshold * bundles;
        return Math.Max(0, Math.Round(bundleSubtotal - fixedPrice * bundles, 2));
    }

    private static bool GroupAppliesAt(MixMatchGroup g, DateTime now)
    {
        var date = DateOnly.FromDateTime(now);
        if (g.StartDate is { } sd && date < sd) return false;
        if (g.EndDate is { } ed && date > ed) return false;

        if (g.DaysOfWeek != DaysOfWeekFlags.All && g.DaysOfWeek != 0)
        {
            var bit = (DaysOfWeekFlags)(1 << (int)now.DayOfWeek);
            if ((g.DaysOfWeek & bit) == 0) return false;
        }

        if (g.StartTime is { } st && g.EndTime is { } et)
        {
            var t = TimeOnly.FromDateTime(now);
            if (st <= et)
            {
                if (t < st || t > et) return false;
            }
            else
            {
                // 22:00 → 02:00 wrap
                if (t < st && t > et) return false;
            }
        }
        return true;
    }
}
