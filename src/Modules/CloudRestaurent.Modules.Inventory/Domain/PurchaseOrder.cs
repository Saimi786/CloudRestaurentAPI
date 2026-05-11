using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Inventory.Domain;

public enum PurchaseOrderStatus
{
    Draft = 0,
    Sent = 1,
    PartialReceived = 2,
    Closed = 3,
    Cancelled = 4
}

/// <summary>
/// A purchase order to a supplier. Goes Draft → Sent → (Partial)Received → Closed.
/// Receiving happens incrementally via Receive() — each call may close the PO if all lines are full.
/// </summary>
public class PurchaseOrder : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid BranchId { get; private set; }
    public Guid SupplierId { get; private set; }
    public string Number { get; private set; } = null!;
    public PurchaseOrderStatus Status { get; private set; }
    public DateOnly? ExpectedDate { get; private set; }
    public string Currency { get; private set; } = null!;
    public string? Notes { get; private set; }

    public decimal SubtotalAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal GrandTotalAmount { get; private set; }

    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    private readonly List<PurchaseOrderLine> _lines = new();
    public IReadOnlyCollection<PurchaseOrderLine> Lines => _lines;

    private PurchaseOrder() { }

    public PurchaseOrder(
        Guid id, Guid tenantId, Guid branchId, Guid supplierId,
        string number, string currency, DateOnly? expectedDate, string? notes)
    {
        Id = id;
        TenantId = tenantId;
        BranchId = branchId;
        SupplierId = supplierId;
        Number = number;
        Currency = currency;
        ExpectedDate = expectedDate;
        Notes = notes;
        Status = PurchaseOrderStatus.Draft;
    }

    public void AddLine(PurchaseOrderLine line)
    {
        EnsureEditable();
        _lines.Add(line);
    }

    public void ReplaceLines(IEnumerable<PurchaseOrderLine> lines)
    {
        EnsureEditable();
        _lines.Clear();
        foreach (var l in lines) _lines.Add(l);
    }

    public void Update(DateOnly? expectedDate, string? notes)
    {
        EnsureEditable();
        ExpectedDate = expectedDate;
        Notes = notes;
    }

    public void RecomputeTotals(decimal subtotal, decimal tax)
    {
        SubtotalAmount = subtotal;
        TaxAmount = tax;
        GrandTotalAmount = subtotal + tax;
    }

    public void Send()
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException($"Cannot send PO in status {Status}.");
        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot send a PO with no lines.");
        Status = PurchaseOrderStatus.Sent;
        SentAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status is PurchaseOrderStatus.Closed or PurchaseOrderStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel a {Status} PO.");
        Status = PurchaseOrderStatus.Cancelled;
        ClosedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Apply received quantities (already validated by the handler). Caller invokes this
    /// after creating StockMovements so the PO state mirrors the GRN.
    /// </summary>
    public void ApplyReceived()
    {
        if (Status is not (PurchaseOrderStatus.Sent or PurchaseOrderStatus.PartialReceived))
            throw new InvalidOperationException($"Cannot receive against PO in status {Status}.");

        var allFull = _lines.All(l => l.ReceivedQuantity >= l.OrderedQuantity);
        if (allFull)
        {
            Status = PurchaseOrderStatus.Closed;
            ClosedAt = DateTimeOffset.UtcNow;
        }
        else if (_lines.Any(l => l.ReceivedQuantity > 0))
        {
            Status = PurchaseOrderStatus.PartialReceived;
        }
    }

    private void EnsureEditable()
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException(
                $"PO is {Status}; only Draft POs can be edited.");
    }
}

public class PurchaseOrderLine : Entity<Guid>
{
    public Guid PurchaseOrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;        // snapshot
    public string ProductSku { get; private set; } = null!;
    public Guid UnitId { get; private set; }
    public decimal OrderedQuantity { get; private set; }
    public decimal ReceivedQuantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal LineTotal { get; private set; }
    public string? Notes { get; private set; }

    private PurchaseOrderLine() { }

    public PurchaseOrderLine(
        Guid id, Guid poId, Guid productId, string sku, string name, Guid unitId,
        decimal orderedQuantity, decimal unitCost, string? notes)
    {
        if (orderedQuantity <= 0) throw new ArgumentOutOfRangeException(nameof(orderedQuantity));
        if (unitCost < 0) throw new ArgumentOutOfRangeException(nameof(unitCost));

        Id = id;
        PurchaseOrderId = poId;
        ProductId = productId;
        ProductSku = sku;
        ProductName = name;
        UnitId = unitId;
        OrderedQuantity = orderedQuantity;
        UnitCost = unitCost;
        LineTotal = Math.Round(orderedQuantity * unitCost, 4);
        ReceivedQuantity = 0;
        Notes = notes;
    }

    public void RecordReceipt(decimal qty)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        if (ReceivedQuantity + qty - OrderedQuantity > 0.0001m)
            throw new InvalidOperationException(
                $"Receipt of {qty} would exceed ordered {OrderedQuantity} (already received {ReceivedQuantity}).");
        ReceivedQuantity += qty;
    }
}
