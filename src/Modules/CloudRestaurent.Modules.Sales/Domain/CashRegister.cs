using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Sales.Domain;

/// <summary>
/// A till — physical or virtual — where cash is collected. A Branch can have many registers
/// (POS-1, Bar, Drive-thru). One user can have at most one open shift per register.
/// </summary>
public class CashRegister : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid BranchId { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private CashRegister() { }

    public CashRegister(Guid id, Guid tenantId, Guid branchId, string code, string name)
    {
        Id = id;
        TenantId = tenantId;
        BranchId = branchId;
        Code = code;
        Name = name;
        IsActive = true;
    }

    public void Update(string code, string name) { Code = code; Name = name; }
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
