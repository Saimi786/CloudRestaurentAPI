using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Catalog.Domain;

/// <summary>
/// One ingredient line of a Combo product. The parent (a <see cref="Product"/>
/// with <c>Type = Combo</c>) sells at its own BasePrice; selling it expands to
/// these component lines for inventory deduction (via the same StockMovement
/// pipeline recipes use). Quantity is in the component product's own unit —
/// no unit conversion at this layer; it would have happened when the combo
/// was authored.
/// </summary>
public class ComboComponent : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid ParentProductId { get; private set; }
    public Guid ComponentProductId { get; private set; }
    public decimal Quantity { get; private set; }

    private ComboComponent() { }

    public ComboComponent(Guid id, Guid tenantId, Guid parentProductId, Guid componentProductId, decimal quantity)
    {
        if (parentProductId == componentProductId)
            throw new ArgumentException("A combo cannot include itself as a component.", nameof(componentProductId));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be > 0.");

        Id = id;
        TenantId = tenantId;
        ParentProductId = parentProductId;
        ComponentProductId = componentProductId;
        Quantity = quantity;
    }

    public void UpdateQuantity(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be > 0.");
        Quantity = quantity;
    }
}
