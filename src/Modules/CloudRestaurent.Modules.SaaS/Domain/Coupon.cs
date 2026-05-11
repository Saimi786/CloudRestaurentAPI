using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.SaaS.Domain;

/// <summary>
/// A promotional coupon that reduces a Subscription's recurring price by a percent.
/// Single-use per tenant unless <see cref="MaxUses"/> is null (unlimited).
/// </summary>
public class Coupon : Entity<Guid>
{
    public string Code { get; private set; } = null!;
    public decimal DiscountPercent { get; private set; }
    public int? MaxUses { get; private set; }
    public int UsesCount { get; private set; }
    public DateOnly? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; }

    private Coupon() { }

    public Coupon(Guid id, string code, decimal discountPercent, int? maxUses, DateOnly? expiresAt)
    {
        if (discountPercent <= 0 || discountPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(discountPercent), "Must be 0-100.");
        Id = id;
        Code = code;
        DiscountPercent = discountPercent;
        MaxUses = maxUses;
        ExpiresAt = expiresAt;
        IsActive = true;
    }

    public bool IsRedeemable(DateOnly today) =>
        IsActive && (ExpiresAt is null || today <= ExpiresAt) && (MaxUses is null || UsesCount < MaxUses);

    public void Redeem()
    {
        if (!IsActive) throw new InvalidOperationException("Coupon inactive.");
        UsesCount++;
    }

    public void Deactivate() => IsActive = false;
}
