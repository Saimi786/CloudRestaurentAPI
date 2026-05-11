namespace CloudRestaurent.Modules.Pricing.Domain;

/// <summary>
/// How the discount is applied when a customer has the required N items from the group.
/// </summary>
public enum MixMatchType
{
    /// <summary>Flat amount off the group total. "$5 off when you buy any 3."</summary>
    DiscountAmount = 0,

    /// <summary>% off the group total. "10% off when you buy any 3."</summary>
    PercentDiscount = 1,

    /// <summary>Set the group total to this fixed price. "Any 3 for $20."</summary>
    FixedPrice = 2
}
