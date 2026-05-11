using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Sales.Domain;

public enum ShiftStatus { Open = 0, Closed = 1 }

public enum ShiftMovementType
{
    /// <summary>Cash-in from a sale paid in cash.</summary>
    Sale = 0,
    /// <summary>Cash refund out to a customer.</summary>
    Refund = 1,
    /// <summary>Cashier paid out a small expense from the till (a paid-out / "PO" slip).</summary>
    PaidOut = 2,
    /// <summary>Manual cash drop or supplemental float added during shift.</summary>
    CashIn = 3,
    /// <summary>Manual cash pull / safe drop during shift (out, not for refund).</summary>
    CashOut = 4
}

/// <summary>
/// One shift = one cashier's session at one register from open to close. The Z-report
/// is read off this aggregate's movements. Expected close = opening + Σ(cash-in flows) − Σ(cash-out flows).
/// </summary>
public class CashRegisterShift : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid CashRegisterId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid OpenedByUserId { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public decimal OpeningAmount { get; private set; }
    public string Currency { get; private set; } = null!;

    public Guid? ClosedByUserId { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public decimal? DeclaredClosingAmount { get; private set; }
    public decimal? ExpectedClosingAmount { get; private set; }
    public decimal? OverShortAmount { get; private set; }
    public string? Notes { get; private set; }
    public ShiftStatus Status { get; private set; }

    private readonly List<CashRegisterShiftMovement> _movements = new();
    public IReadOnlyCollection<CashRegisterShiftMovement> Movements => _movements;

    private CashRegisterShift() { }

    public CashRegisterShift(
        Guid id, Guid tenantId, Guid registerId, Guid branchId,
        Guid openedByUserId, decimal openingAmount, string currency)
    {
        if (openingAmount < 0) throw new ArgumentOutOfRangeException(nameof(openingAmount));
        Id = id;
        TenantId = tenantId;
        CashRegisterId = registerId;
        BranchId = branchId;
        OpenedByUserId = openedByUserId;
        OpenedAt = DateTime.UtcNow;
        OpeningAmount = openingAmount;
        Currency = currency;
        Status = ShiftStatus.Open;
    }

    public void RecordMovement(ShiftMovementType type, decimal amount, Guid? sourceId, string? reference, string? notes)
    {
        if (Status != ShiftStatus.Open)
            throw new InvalidOperationException("Cannot record movement on a closed shift.");
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be non-negative.");

        _movements.Add(new CashRegisterShiftMovement(
            Guid.NewGuid(), Id, type, amount, sourceId, reference, notes));
    }

    public void Close(Guid closedByUserId, decimal declaredClosingAmount, string? notes)
    {
        if (Status != ShiftStatus.Open)
            throw new InvalidOperationException("Shift is already closed.");
        if (declaredClosingAmount < 0) throw new ArgumentOutOfRangeException(nameof(declaredClosingAmount));

        var inflow = _movements
            .Where(m => m.Type is ShiftMovementType.Sale or ShiftMovementType.CashIn)
            .Sum(m => m.Amount);
        var outflow = _movements
            .Where(m => m.Type is ShiftMovementType.Refund or ShiftMovementType.PaidOut or ShiftMovementType.CashOut)
            .Sum(m => m.Amount);
        var expected = OpeningAmount + inflow - outflow;

        ExpectedClosingAmount = expected;
        DeclaredClosingAmount = declaredClosingAmount;
        OverShortAmount = declaredClosingAmount - expected;
        ClosedByUserId = closedByUserId;
        ClosedAt = DateTime.UtcNow;
        Notes = notes;
        Status = ShiftStatus.Closed;
    }
}

public class CashRegisterShiftMovement : Entity<Guid>
{
    public Guid ShiftId { get; private set; }
    public ShiftMovementType Type { get; private set; }
    public decimal Amount { get; private set; }
    public Guid? SourceId { get; private set; }
    public string? Reference { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private CashRegisterShiftMovement() { }

    public CashRegisterShiftMovement(Guid id, Guid shiftId, ShiftMovementType type, decimal amount,
        Guid? sourceId, string? reference, string? notes)
    {
        Id = id;
        ShiftId = shiftId;
        Type = type;
        Amount = amount;
        SourceId = sourceId;
        Reference = reference;
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
    }
}
