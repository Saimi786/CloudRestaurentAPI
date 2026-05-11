using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Catalog.Domain;

public class Category : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid? ParentCategoryId { get; private set; }
    public Guid? KitchenStationId { get; private set; }
    public string Name { get; private set; } = null!;
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    private Category() { }

    public Category(Guid id, Guid tenantId, string name, int displayOrder, Guid? parentCategoryId = null)
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
        DisplayOrder = displayOrder;
        ParentCategoryId = parentCategoryId;
        IsActive = true;
    }

    public void Update(string name, int displayOrder, Guid? parentCategoryId)
    {
        if (parentCategoryId == Id)
            throw new InvalidOperationException("A category cannot be its own parent.");
        Name = name;
        DisplayOrder = displayOrder;
        ParentCategoryId = parentCategoryId;
    }

    public void SetKitchenStation(Guid? stationId) => KitchenStationId = stationId;

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
