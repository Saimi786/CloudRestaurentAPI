using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Inventory.Domain;

public enum WasteReason
{
    Spoilage = 0,
    Breakage = 1,
    Theft = 2,
    PrepError = 3,
    Other = 99
}

/// <summary>
/// Itemized record of stock written off as waste. Persists alongside the StockMovement
/// (type=Wastage) so reports can break "why did we lose stock" down by reason.
/// </summary>
public class WasteLog : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid BranchId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid UnitId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal QuantityInProductUnit { get; private set; }
    public WasteReason Reason { get; private set; }
    public string? Notes { get; private set; }
    public Guid? StockMovementId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    private WasteLog() { }

    public WasteLog(
        Guid id, Guid tenantId, Guid branchId, Guid productId, Guid unitId,
        decimal quantity, decimal quantityInProductUnit,
        WasteReason reason, string? notes, Guid? stockMovementId,
        Guid createdByUserId, DateTimeOffset occurredAt)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        Id = id;
        TenantId = tenantId;
        BranchId = branchId;
        ProductId = productId;
        UnitId = unitId;
        Quantity = quantity;
        QuantityInProductUnit = quantityInProductUnit;
        Reason = reason;
        Notes = notes;
        StockMovementId = stockMovementId;
        CreatedByUserId = createdByUserId;
        OccurredAt = occurredAt;
    }
}
