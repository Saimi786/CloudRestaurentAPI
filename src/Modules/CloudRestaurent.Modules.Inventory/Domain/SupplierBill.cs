using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Inventory.Domain;

public enum SupplierBillStatus { Open = 0, PartiallyPaid = 1, Paid = 2, Cancelled = 3 }
public enum SupplierBillPaymentMethod { Cash = 0, Card = 1, BankTransfer = 2, Wallet = 3 }

/// <summary>
/// Three-way match outcome between PO ordered qty × cost, GRN received qty × cost, and the
/// supplier's bill total.
/// </summary>
public enum BillMatchStatus
{
    /// <summary>No PO linked, or not yet matched.</summary>
    NotMatched = 0,
    /// <summary>Bill total agrees with received-qty × PO unit cost (within tolerance).</summary>
    Matched = 1,
    /// <summary>Bill total > expected — supplier may have over-billed.</summary>
    OverBilled = 2,
    /// <summary>Bill total < expected — supplier may have under-billed.</summary>
    UnderBilled = 3,
    /// <summary>Bill manually flagged disputed by ops.</summary>
    Disputed = 4
}

/// <summary>
/// A bill from a supplier (AP voucher). Auto-created when a PO is fully received.
/// Tracks the running paid total; status flips to Paid when fully settled.
/// </summary>
public class SupplierBill : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid SupplierId { get; private set; }
    public Guid? PurchaseOrderId { get; private set; }
    public Guid BranchId { get; private set; }
    public string Number { get; private set; } = null!;
    public string? SupplierBillReference { get; private set; }   // their invoice number
    public DateOnly BillDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public string Currency { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public SupplierBillStatus Status { get; private set; }
    public string? Notes { get; private set; }

    // 3-way match -----------------------------------------------------
    public BillMatchStatus MatchStatus { get; private set; } = BillMatchStatus.NotMatched;
    /// <summary>Expected total based on PO ordered/received × unit cost (computed at match time).</summary>
    public decimal? ExpectedAmount { get; private set; }
    /// <summary>Bill amount − ExpectedAmount. Positive = over-billed; negative = under-billed.</summary>
    public decimal? DiscrepancyAmount { get; private set; }
    public string? DiscrepancyReason { get; private set; }
    public DateTimeOffset? MatchedAt { get; private set; }
    public Guid? MatchedByUserId { get; private set; }

    private readonly List<SupplierBillPayment> _payments = new();
    public IReadOnlyCollection<SupplierBillPayment> Payments => _payments;

    private SupplierBill() { }

    public SupplierBill(
        Guid id, Guid tenantId, Guid supplierId, Guid? purchaseOrderId, Guid branchId,
        string number, string? supplierBillReference,
        DateOnly billDate, DateOnly? dueDate,
        decimal amount, string currency, string? notes)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Id = id;
        TenantId = tenantId;
        SupplierId = supplierId;
        PurchaseOrderId = purchaseOrderId;
        BranchId = branchId;
        Number = number;
        SupplierBillReference = supplierBillReference;
        BillDate = billDate;
        DueDate = dueDate;
        Amount = amount;
        Currency = currency;
        Notes = notes;
        Status = SupplierBillStatus.Open;
    }

    public void AddPayment(SupplierBillPayment payment)
    {
        if (Status is SupplierBillStatus.Paid or SupplierBillStatus.Cancelled)
            throw new InvalidOperationException($"Bill is {Status}.");
        _payments.Add(payment);
        PaidAmount += payment.Amount;
        if (PaidAmount >= Amount - 0.0001m) Status = SupplierBillStatus.Paid;
        else if (PaidAmount > 0) Status = SupplierBillStatus.PartiallyPaid;
    }

    public decimal Outstanding() => Math.Max(0, Amount - PaidAmount);

    public void UpdateBillDetails(
        decimal amount, string? supplierBillReference, DateOnly billDate, DateOnly? dueDate, string? notes)
    {
        if (Status is SupplierBillStatus.Paid or SupplierBillStatus.Cancelled)
            throw new InvalidOperationException($"Bill is {Status}; cannot edit.");
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (amount < PaidAmount)
            throw new InvalidOperationException(
                $"New amount {amount} is less than already paid {PaidAmount}.");
        Amount = amount;
        SupplierBillReference = supplierBillReference;
        BillDate = billDate;
        DueDate = dueDate;
        Notes = notes;
        // Editing the amount invalidates a previous match.
        MatchStatus = BillMatchStatus.NotMatched;
        ExpectedAmount = null;
        DiscrepancyAmount = null;
        MatchedAt = null;
        MatchedByUserId = null;
    }

    public void RecordMatch(
        decimal expected, decimal tolerance, Guid userId, string? overrideReason)
    {
        ExpectedAmount = expected;
        var diff = Amount - expected;
        DiscrepancyAmount = diff;
        if (Math.Abs(diff) <= tolerance)
            MatchStatus = BillMatchStatus.Matched;
        else if (diff > 0)
            MatchStatus = BillMatchStatus.OverBilled;
        else
            MatchStatus = BillMatchStatus.UnderBilled;
        DiscrepancyReason = overrideReason;
        MatchedAt = DateTimeOffset.UtcNow;
        MatchedByUserId = userId;
    }

    public void FlagDisputed(Guid userId, string reason)
    {
        MatchStatus = BillMatchStatus.Disputed;
        DiscrepancyReason = reason;
        MatchedAt = DateTimeOffset.UtcNow;
        MatchedByUserId = userId;
    }
}

public class SupplierBillPayment : Entity<Guid>
{
    public Guid BillId { get; private set; }
    public decimal Amount { get; private set; }
    public SupplierBillPaymentMethod Method { get; private set; }
    public string? Reference { get; private set; }
    public DateTimeOffset PaidAt { get; private set; }
    public Guid PaidByUserId { get; private set; }

    private SupplierBillPayment() { }

    public SupplierBillPayment(
        Guid id, Guid billId, decimal amount, SupplierBillPaymentMethod method,
        string? reference, Guid paidByUserId, DateTimeOffset paidAt)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Id = id;
        BillId = billId;
        Amount = amount;
        Method = method;
        Reference = reference;
        PaidByUserId = paidByUserId;
        PaidAt = paidAt;
    }
}
