using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Catalog.Domain.Modifiers;

/// <summary>
/// Many-to-many link between a Product (menu item) and a ModifierGroup that applies to it.
/// </summary>
public class ProductModifierGroup : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; private set; }
    public Guid ModifierGroupId { get; private set; }
    public int DisplayOrder { get; private set; }

    private ProductModifierGroup() { }

    public ProductModifierGroup(Guid id, Guid tenantId, Guid productId, Guid modifierGroupId, int displayOrder)
    {
        Id = id;
        TenantId = tenantId;
        ProductId = productId;
        ModifierGroupId = modifierGroupId;
        DisplayOrder = displayOrder;
    }
}
