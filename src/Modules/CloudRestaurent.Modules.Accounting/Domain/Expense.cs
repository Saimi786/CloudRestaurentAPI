using CloudRestaurent.Domain.Common;
using CloudRestaurent.Modules.Sales.Domain;

namespace CloudRestaurent.Modules.Accounting.Domain;

/// <summary>
/// Cash/bank money going out for operating expenses (rent, utilities, supplies, ad-hoc paid-outs).
/// Posted to GL as: debit chosen Expense account, credit Cash/Bank by payment method.
/// If paid in cash from an open till, also recorded as PaidOut on the cashier's shift.
/// </summary>
public class Expense : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid BranchId { get; private set; }
    public Guid ExpenseAccountId { get; private set; }
    public string Reference { get; private set; } = null!;
    public string? Description { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public PaymentMethod Method { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private Expense() { }

    public Expense(
        Guid id, Guid tenantId, Guid branchId, Guid expenseAccountId,
        string reference, string? description, decimal amount, string currency,
        PaymentMethod method, Guid createdByUserId, DateTimeOffset? occurredAt)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Id = id;
        TenantId = tenantId;
        BranchId = branchId;
        ExpenseAccountId = expenseAccountId;
        Reference = reference;
        Description = description;
        Amount = amount;
        Currency = currency;
        Method = method;
        CreatedByUserId = createdByUserId;
        OccurredAt = occurredAt ?? DateTimeOffset.UtcNow;
    }
}
