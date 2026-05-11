using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Catalog.Domain;

public class Brand : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; }

    private Brand() { }

    public Brand(Guid id, Guid tenantId, string name, string? description, string? imageUrl)
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
        Description = description;
        ImageUrl = imageUrl;
        IsActive = true;
    }

    public void Update(string name, string? description, string? imageUrl)
    {
        Name = name;
        Description = description;
        ImageUrl = imageUrl;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
