namespace CloudRestaurent.Modules.Inventory.Domain;

public enum StockMovementType
{
    Purchase = 0,    // received from supplier — stock in
    Adjustment = 1,  // manual correction — can be in or out
    Wastage = 2,     // spoilage / damage / loss — stock out
    Sale = 3,        // sold via POS — stock out (reserved; POS not implemented yet)
    TransferIn = 4,  // received from another branch (reserved)
    TransferOut = 5  // sent to another branch (reserved)
}

public static class StockMovementTypeExtensions
{
    /// <summary>
    /// Sign convention enforced by the system: a Purchase always increases stock,
    /// a Wastage always decreases. Adjustments use the sign provided by the caller.
    /// </summary>
    public static int FixedSign(this StockMovementType type) => type switch
    {
        StockMovementType.Purchase   =>  1,
        StockMovementType.Wastage    => -1,
        StockMovementType.Sale       => -1,
        StockMovementType.TransferIn =>  1,
        StockMovementType.TransferOut => -1,
        StockMovementType.Adjustment => 0,
        _ => 0
    };
}
