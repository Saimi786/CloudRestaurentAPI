using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Catalog.Domain;

public class UnitGroup : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private UnitGroup() { }

    public UnitGroup(Guid id, Guid tenantId, string name)
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
        IsActive = true;
    }

    public void Update(string name) => Name = name;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
