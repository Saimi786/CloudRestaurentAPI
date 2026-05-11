namespace CloudRestaurent.Application.Common.Abstractions;

/// <summary>
/// Resolves the effective unit price for a Product at a Branch at a given time.
/// POS and order-line creation call this. Returns the most-specific active price rule's
/// override, or the Product's BasePrice if no rule applies.
/// </summary>
public interface IPriceResolver
{
    Task<ResolvedPrice> ResolveAsync(
        Guid productId,
        Guid? branchId,
        DateTime atLocal,
        CancellationToken ct);
}

public sealed record ResolvedPrice(
    decimal Amount,
    string Currency,
    Guid? AppliedRuleId,
    string? AppliedRuleName);
