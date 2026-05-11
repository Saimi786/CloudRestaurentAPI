namespace CloudRestaurent.Modules.Catalog.Domain;

/// <summary>
/// Mirrors UltimatePOS's <c>products.type</c> enum. Drives stock behaviour and
/// menu rendering: Service items skip stock deduction; Combos contain other
/// products; Modifiers attach to other items.
/// </summary>
public enum ProductType
{
    Goods = 0,
    Service = 1,
    Combo = 2,
    Modifier = 3
}
