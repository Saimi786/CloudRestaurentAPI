using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Inventory.Domain;

/// <summary>
/// Current quantity of a Product at a Branch, expressed in the Product's primary unit.
/// </summary>
public class StockBalance : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid BranchId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTimeOffset LastMovementAt { get; private set; }

    private StockBalance() { }

    public StockBalance(Guid id, Guid tenantId, Guid branchId, Guid productId)
    {
        Id = id;
        TenantId = tenantId;
        BranchId = branchId;
        ProductId = productId;
        Quantity = 0m;
        LastMovementAt = DateTimeOffset.UtcNow;
    }

    public void Apply(decimal deltaInProductUnit, DateTimeOffset occurredAt)
    {
        Quantity += deltaInProductUnit;
        if (occurredAt > LastMovementAt) LastMovementAt = occurredAt;
    }
}
