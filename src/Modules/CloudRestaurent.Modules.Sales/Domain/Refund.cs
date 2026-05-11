using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Sales.Domain;

/// <summary>
/// Money returned to a customer against a closed Order. Optional return-lines
/// describe which products to put back in stock (full or partial qty).
/// </summary>
public class Refund : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid RefundedByUserId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public PaymentMethod Method { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset RefundedAt { get; private set; }

    private readonly List<RefundLine> _lines = new();
    public IReadOnlyCollection<RefundLine> Lines => _lines;

    private Refund() { }

    public Refund(
        Guid id, Guid tenantId, Guid orderId, Guid branchId, Guid refundedByUserId,
        decimal amount, string currency, PaymentMethod method, string? reason)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Refund amount must be > 0.");
        Id = id;
        TenantId = tenantId;
        OrderId = orderId;
        BranchId = branchId;
        RefundedByUserId = refundedByUserId;
        Amount = amount;
        Currency = currency;
        Method = method;
        Reason = reason;
        RefundedAt = DateTimeOffset.UtcNow;
    }

    public void AddLine(Guid orderLineId, Guid productId, decimal quantity, bool restock)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        _lines.Add(new RefundLine(Guid.NewGuid(), Id, orderLineId, productId, quantity, restock));
    }
}

public class RefundLine : Entity<Guid>
{
    public Guid RefundId { get; private set; }
    public Guid OrderLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public bool Restock { get; private set; }

    private RefundLine() { }

    public RefundLine(Guid id, Guid refundId, Guid orderLineId, Guid productId, decimal qty, bool restock)
    {
        Id = id;
        RefundId = refundId;
        OrderLineId = orderLineId;
        ProductId = productId;
        Quantity = qty;
        Restock = restock;
    }
}
