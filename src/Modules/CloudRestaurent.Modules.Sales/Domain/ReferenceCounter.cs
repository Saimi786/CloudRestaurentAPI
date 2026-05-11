using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Sales.Domain;

/// <summary>
/// Sequential document numbers per (Tenant, Branch, DocumentType). Used to allocate
/// OrderNumber on sales, will also drive Purchase / Expense numbering when those land.
/// Mirrors UltimatePOS's <c>reference_counts</c>.
/// </summary>
public class ReferenceCounter : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid BranchId { get; private set; }
    public string DocumentType { get; private set; } = null!;
    public string Prefix { get; private set; } = null!;
    public long CurrentValue { get; private set; }

    private ReferenceCounter() { }

    public ReferenceCounter(Guid id, Guid tenantId, Guid branchId, string documentType, string prefix)
    {
        Id = id;
        TenantId = tenantId;
        BranchId = branchId;
        DocumentType = documentType;
        Prefix = prefix;
        CurrentValue = 0;
    }

    /// <summary>Increment and return the formatted reference (e.g. "SAL-00042").</summary>
    public string Next()
    {
        CurrentValue++;
        return $"{Prefix}-{CurrentValue:D5}";
    }
}
