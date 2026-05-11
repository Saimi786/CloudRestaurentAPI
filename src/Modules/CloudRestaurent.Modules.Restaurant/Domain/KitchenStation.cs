using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Restaurant.Domain;

/// <summary>
/// A physical kitchen station — Grill, Bar, Cold Station, Bakery, Fry, Expo, etc.
/// Categories route to a station, so all products in that category appear on that
/// station's KDS screen. Mirrors UltimatePOS's <c>kitchen</c> module addon (one of
/// our key restaurant differentiators per RESTAURANT_PLAYBOOK §2.4).
/// </summary>
public class KitchenStation : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid BranchId { get; private set; }
    public string Name { get; private set; } = null!;
    public int DisplayOrder { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public string? PrinterIpAddress { get; private set; }
    public int? PrinterPort { get; private set; }

    private KitchenStation() { }

    public KitchenStation(Guid id, Guid tenantId, Guid branchId, string name, int displayOrder, string? description = null)
    {
        Id = id;
        TenantId = tenantId;
        BranchId = branchId;
        Name = name;
        DisplayOrder = displayOrder;
        Description = description;
        IsActive = true;
    }

    public void Update(string name, int displayOrder, string? description)
    {
        Name = name;
        DisplayOrder = displayOrder;
        Description = description;
    }

    public void SetPrinter(string? ipAddress, int? port)
    {
        PrinterIpAddress = ipAddress;
        PrinterPort = port;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
