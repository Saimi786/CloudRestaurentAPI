using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Domain.Companies;

public class Branch : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public Location Location { get; private set; } = Location.Empty();
    public string? PhoneNumber { get; private set; }
    public bool IsActive { get; private set; }
    public ReceiptTemplate ReceiptTemplate { get; private set; } = ReceiptTemplate.Compact;
    public string? ReceiptFooterText { get; private set; }

    private Branch() { }

    public Branch(Guid id, Guid tenantId, Guid companyId, string name, string code, Location location)
    {
        Id = id;
        TenantId = tenantId;
        CompanyId = companyId;
        Name = name;
        Code = code;
        Location = location;
        IsActive = true;
    }

    public void Update(string name, string code, Location location, string? phoneNumber)
    {
        Name = name;
        Code = code;
        Location = location;
        PhoneNumber = phoneNumber;
    }

    public void SetReceiptOptions(ReceiptTemplate template, string? footerText)
    {
        ReceiptTemplate = template;
        ReceiptFooterText = footerText;
    }

    public void UpdateLocation(Location location) => Location = location;
    public void SetPhoneNumber(string? phone) => PhoneNumber = phone;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
