using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Contacts.Domain;

/// <summary>
/// Customer tier (Regular / Silver / Gold). Drives default discount % applied to
/// any contact in this group. Mirrors UltimatePOS's <c>customer_groups</c> table.
/// </summary>
public class CustomerGroup : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = null!;
    public decimal DiscountPercent { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private CustomerGroup() { }

    public CustomerGroup(Guid id, Guid tenantId, string name, decimal discountPercent, string? description)
    {
        if (discountPercent < 0 || discountPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(discountPercent), "Discount must be 0-100.");
        Id = id;
        TenantId = tenantId;
        Name = name;
        DiscountPercent = discountPercent;
        Description = description;
        IsActive = true;
    }

    public void Update(string name, decimal discountPercent, string? description)
    {
        if (discountPercent < 0 || discountPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(discountPercent), "Discount must be 0-100.");
        Name = name;
        DiscountPercent = discountPercent;
        Description = description;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
