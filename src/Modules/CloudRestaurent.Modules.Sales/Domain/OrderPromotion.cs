using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Sales.Domain;

/// <summary>
/// A Mix &amp; Match (or other) group promotion applied to an Order at recompute time.
/// Recomputed wholesale on each cart change — not user-editable. Sums contribute to
/// Order.PromotionDiscountAmount.
/// </summary>
public class OrderPromotion : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid SourceGroupId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public decimal Amount { get; private set; }

    private OrderPromotion() { }

    public OrderPromotion(Guid id, Guid orderId, Guid sourceGroupId, string name, string? description, decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Id = id;
        OrderId = orderId;
        SourceGroupId = sourceGroupId;
        Name = name;
        Description = description;
        Amount = amount;
    }
}
