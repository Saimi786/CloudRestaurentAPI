using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Sales.Domain;

public class Order : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid BranchId { get; private set; }
    public Guid? TableId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public OrderType Type { get; private set; }
    public OrderStatus Status { get; private set; }
    public string Currency { get; private set; } = "PKR";
    public string? OrderNumber { get; private set; }
    public string? Notes { get; private set; }

    public decimal SubtotalAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal PromotionDiscountAmount { get; private set; }
    public decimal GrandTotalAmount { get; private set; }

    /// <summary>Reward points earned by this order (snapshotted at close).</summary>
    public int RewardPointsEarned { get; private set; }

    /// <summary>Reward points redeemed against this order (set before close).</summary>
    public int RewardPointsRedeemed { get; private set; }

    /// <summary>Currency value of the redeemed points (acts as an additional discount).</summary>
    public decimal RewardPointsRedeemedAmount { get; private set; }

    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    private readonly List<OrderLine> _lines = new();
    public IReadOnlyCollection<OrderLine> Lines => _lines;

    private readonly List<Payment> _payments = new();
    public IReadOnlyCollection<Payment> Payments => _payments;

    private readonly List<OrderPromotion> _promotions = new();
    public IReadOnlyCollection<OrderPromotion> Promotions => _promotions;

    private Order() { }

    public Order(
        Guid id, Guid tenantId, Guid branchId,
        Guid? tableId, Guid? customerId,
        OrderType type, string currency, string? notes)
    {
        Id = id;
        TenantId = tenantId;
        BranchId = branchId;
        TableId = tableId;
        CustomerId = customerId;
        Type = type;
        Status = OrderStatus.Open;
        Currency = currency;
        Notes = notes;
        OpenedAt = DateTimeOffset.UtcNow;
    }

    public void SetOrderNumber(string orderNumber) => OrderNumber = orderNumber;

    public void SetDiscount(decimal discountAmount)
    {
        if (discountAmount < 0) throw new ArgumentOutOfRangeException(nameof(discountAmount));
        DiscountAmount = discountAmount;
        RebuildGrandTotal();
    }

    /// <summary>
    /// Recalculate Subtotal/Tax/GrandTotal from a complete set of lines (loaded from DB).
    /// Discount is preserved unless explicitly changed via <see cref="SetDiscount"/>.
    /// </summary>
    public void RecomputeTotals(IEnumerable<OrderLine> allLines)
    {
        decimal sub = 0, tax = 0;
        foreach (var l in allLines)
        {
            sub += l.LineSubtotal;
            tax += l.TaxAmount;
        }
        SubtotalAmount = sub;
        TaxAmount = tax;
        RebuildGrandTotal();
    }

    public void SetPromotionDiscount(decimal amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        PromotionDiscountAmount = amount;
        RebuildGrandTotal();
    }

    /// <summary>
    /// Sets the points the customer is redeeming on this order and the currency value
    /// those points buy. The amount is folded into the grand total alongside other
    /// discounts. Caller is responsible for validating against business settings + balance.
    /// </summary>
    public void SetRewardRedemption(int points, decimal amount)
    {
        if (points < 0) throw new ArgumentOutOfRangeException(nameof(points));
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        EnsureOpen();
        RewardPointsRedeemed = points;
        RewardPointsRedeemedAmount = amount;
        RebuildGrandTotal();
    }

    /// <summary>
    /// Snapshot the points earned at close. Mirrors UltimatePOS's `rp_earned` column.
    /// </summary>
    public void SetRewardPointsEarned(int points)
    {
        if (points < 0) throw new ArgumentOutOfRangeException(nameof(points));
        RewardPointsEarned = points;
    }

    private void RebuildGrandTotal()
    {
        var grand = SubtotalAmount + TaxAmount - DiscountAmount - PromotionDiscountAmount - RewardPointsRedeemedAmount;
        if (grand < 0) grand = 0;
        GrandTotalAmount = grand;
    }

    public void AddLine(OrderLine line)
    {
        EnsureOpen();
        _lines.Add(line);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureOpen();
        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new InvalidOperationException($"Line {lineId} not in this order.");
        _lines.Remove(line);
    }

    public void AddPayment(Payment payment)
    {
        EnsureOpen();
        if (payment.Amount.Currency != Currency)
            throw new InvalidOperationException(
                $"Payment currency '{payment.Amount.Currency}' does not match order currency '{Currency}'.");
        _payments.Add(payment);
    }

    public void Close()
    {
        EnsureOpen();
        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot close an order with no lines.");
        if (PaidTotal() + 0.0001m < GrandTotalAmount)
            throw new InvalidOperationException(
                $"Order is not fully paid. Total {GrandTotalAmount:0.00}, paid {PaidTotal():0.00}.");

        Status = OrderStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Flip status without touching the in-memory navigation collections.
    /// Used by the close handler when lines/payments live in the DB but aren't loaded
    /// into the entity (the handler does its own lines-exist + paid-in-full checks via SQL).
    /// </summary>
    public void MarkClosed()
    {
        EnsureOpen();
        Status = OrderStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
    }

    public void Void()
    {
        EnsureOpen();
        Status = OrderStatus.Voided;
        ClosedAt = DateTimeOffset.UtcNow;
    }

    public decimal PaidTotal() => _payments.Sum(p => p.Amount.Amount);
    public decimal Balance() => GrandTotalAmount - PaidTotal();

    private void EnsureOpen()
    {
        if (Status != OrderStatus.Open)
            throw new InvalidOperationException(
                $"Order is {Status}; only Open orders can be modified.");
    }
}
