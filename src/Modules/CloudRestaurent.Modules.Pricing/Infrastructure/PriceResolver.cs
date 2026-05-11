using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Pricing.Domain;
using Microsoft.EntityFrameworkCore;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;

namespace CloudRestaurent.Modules.Pricing.Infrastructure;

public sealed class PriceResolver(IAppDbContext db) : IPriceResolver
{
    public async Task<ResolvedPrice> ResolveAsync(
        Guid productId, Guid? branchId, DateTime atLocal, CancellationToken ct)
    {
        var product = await db.Set<Product>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new NotFoundException("Product", productId);

        // Pull candidate rules: active + same product + (branch matches OR null)
        var candidates = await db.Set<PriceRule>().AsNoTracking()
            .Where(r => r.IsActive && r.ProductId == productId &&
                        (r.BranchId == null || r.BranchId == branchId))
            .ToListAsync(ct);

        // Filter in-memory by time/day window (TimeOnly comparisons EF can struggle with mixed shapes)
        var matching = candidates
            .Where(r => r.MatchesContext(branchId, atLocal))
            .OrderByDescending(r => r.BranchId.HasValue) // branch-specific beats branch-null
            .ThenByDescending(r => r.Priority)
            .ThenByDescending(r => r.StartTime.HasValue)  // time-windowed beats all-day
            .FirstOrDefault();

        if (matching is null)
            return new ResolvedPrice(
                product.BasePrice.Amount, product.BasePrice.Currency,
                AppliedRuleId: null, AppliedRuleName: null);

        return new ResolvedPrice(
            matching.OverridePrice.Amount, matching.OverridePrice.Currency,
            matching.Id, matching.Name);
    }
}
