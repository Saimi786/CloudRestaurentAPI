using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Sales.Domain;

/// <summary>
/// Snapshot of a Modifier applied to an OrderLine. Stores the name and price
/// at order time so historical orders are unaffected by later modifier edits.
/// </summary>
public class OrderLineModifier : Entity<Guid>
{
    public Guid OrderLineId { get; private set; }
    public Guid ModifierId { get; private set; }
    public string Name { get; private set; } = null!;
    public Money PriceAdjustment { get; private set; }

    private OrderLineModifier() { }

    public OrderLineModifier(Guid id, Guid orderLineId, Guid modifierId, string name, Money priceAdjustment)
    {
        Id = id;
        OrderLineId = orderLineId;
        ModifierId = modifierId;
        Name = name;
        PriceAdjustment = priceAdjustment;
    }
}
