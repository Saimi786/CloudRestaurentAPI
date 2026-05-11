using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Inventory.Domain;

/// <summary>
/// Immutable audit row for every change to a Product's stock at a Branch.
/// Records both the original quantity (in whatever unit the user entered)
/// and the normalized quantity in the Product's base unit so that reports
/// and balance math are consistent regardless of input unit.
/// </summary>
public class StockMovement : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid BranchId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid UnitId { get; private set; }
    public StockMovementType Type { get; private set; }

    /// <summary>Signed quantity as the user entered it, in <see cref="UnitId"/>.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>Signed quantity normalized to the Product's primary unit (used for balance math).</summary>
    public decimal QuantityInProductUnit { get; private set; }

    public string? Reference { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    private StockMovement() { }

    public StockMovement(
        Guid id,
        Guid tenantId,
        Guid branchId,
        Guid productId,
        Guid unitId,
        StockMovementType type,
        decimal quantity,
        decimal quantityInProductUnit,
        string? reference,
        string? notes,
        DateTimeOffset occurredAt)
    {
        Id = id;
        TenantId = tenantId;
        BranchId = branchId;
        ProductId = productId;
        UnitId = unitId;
        Type = type;
        Quantity = quantity;
        QuantityInProductUnit = quantityInProductUnit;
        Reference = reference;
        Notes = notes;
        OccurredAt = occurredAt;
    }
}
