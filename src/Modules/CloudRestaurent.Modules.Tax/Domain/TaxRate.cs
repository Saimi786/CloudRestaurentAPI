using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Tax.Domain;

/// <summary>
/// A single tax percentage applicable to products. Mirrors UltimatePOS's
/// <c>tax_rates</c> table. Tax-group composition (CGST + SGST etc.) is deferred.
/// </summary>
public class TaxRate : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = null!;
    public decimal Percentage { get; private set; }
    public bool IsCompound { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }

    private TaxRate() { }

    public TaxRate(Guid id, Guid tenantId, string name, decimal percentage, bool isCompound = false)
    {
        if (percentage < 0 || percentage > 100)
            throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be 0-100.");
        Id = id;
        TenantId = tenantId;
        Name = name;
        Percentage = percentage;
        IsCompound = isCompound;
        IsActive = true;
    }

    public void Update(string name, decimal percentage, bool isCompound)
    {
        if (percentage < 0 || percentage > 100)
            throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be 0-100.");
        Name = name;
        Percentage = percentage;
        IsCompound = isCompound;
    }

    public void MarkAsDefault() => IsDefault = true;
    public void UnmarkDefault() => IsDefault = false;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
