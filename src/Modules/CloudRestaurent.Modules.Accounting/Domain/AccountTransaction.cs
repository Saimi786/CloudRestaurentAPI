using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Accounting.Domain;

/// <summary>
/// A single ledger posting. Every business event creates a balanced batch of these
/// (debits = credits). The (SourceType, SourceId) pair lets us trace back to the
/// originating Sale/Purchase/Payment.
/// </summary>
public class AccountTransaction : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid AccountId { get; private set; }
    public LedgerSide Side { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "PKR";
    public DateTimeOffset OperationDate { get; private set; }
    public string SourceType { get; private set; } = null!;   // "Sale", "Purchase", "Payment", "Manual"
    public Guid? SourceId { get; private set; }
    public string? Description { get; private set; }
    public string? BatchId { get; private set; }              // groups debits + credits of one event

    private AccountTransaction() { }

    public AccountTransaction(
        Guid id, Guid tenantId, Guid accountId, LedgerSide side,
        decimal amount, string currency,
        string sourceType, Guid? sourceId, string? description, string? batchId,
        DateTimeOffset? operationDate = null)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be >= 0.");
        Id = id;
        TenantId = tenantId;
        AccountId = accountId;
        Side = side;
        Amount = amount;
        Currency = currency;
        SourceType = sourceType;
        SourceId = sourceId;
        Description = description;
        BatchId = batchId;
        OperationDate = operationDate ?? DateTimeOffset.UtcNow;
    }
}
