using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Restaurant.Domain;

public class FloorPlan : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid BranchId { get; private set; }
    public string Name { get; private set; } = null!;
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    private FloorPlan() { }

    public FloorPlan(Guid id, Guid tenantId, Guid branchId, string name, int displayOrder)
    {
        Id = id;
        TenantId = tenantId;
        BranchId = branchId;
        Name = name;
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public void Update(string name, int displayOrder)
    {
        Name = name;
        DisplayOrder = displayOrder;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
