using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Catalog.Domain.Modifiers;

public class Modifier : Entity<Guid>
{
    public Guid ModifierGroupId { get; private set; }
    public string Name { get; private set; } = null!;

    /// <summary>How much this modifier adds (or subtracts) from the order line.</summary>
    public Money PriceAdjustment { get; private set; }

    public int DisplayOrder { get; private set; }
    public bool IsDefault { get; private set; }

    private Modifier() { }

    public Modifier(
        Guid id,
        Guid modifierGroupId,
        string name,
        Money priceAdjustment,
        int displayOrder,
        bool isDefault)
    {
        Id = id;
        ModifierGroupId = modifierGroupId;
        Name = name;
        PriceAdjustment = priceAdjustment;
        DisplayOrder = displayOrder;
        IsDefault = isDefault;
    }
}
