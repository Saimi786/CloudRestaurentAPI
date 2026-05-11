using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Accounting.Domain;

/// <summary>
/// Chart-of-accounts entry. Every business event (sale, purchase, payment) posts
/// debit/credit rows against accounts. Mirrors UltimatePOS's <c>accounts</c> table.
/// </summary>
public class Account : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Code { get; private set; } = null!;        // "1000", "4000"
    public string Name { get; private set; } = null!;        // "Cash", "Sales Revenue"
    public AccountClass Class { get; private set; }
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }               // true = seeded by platform, can't delete
    public bool IsCashOrBank { get; private set; }           // payment accounts that POS can debit
    public bool IsActive { get; private set; }

    private Account() { }

    public Account(Guid id, Guid tenantId, string code, string name, AccountClass @class,
        bool isSystem = false, bool isCashOrBank = false, string? description = null)
    {
        Id = id;
        TenantId = tenantId;
        Code = code;
        Name = name;
        Class = @class;
        IsSystem = isSystem;
        IsCashOrBank = isCashOrBank;
        Description = description;
        IsActive = true;
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
