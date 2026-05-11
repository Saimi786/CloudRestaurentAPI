using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Domain.Companies;

public class Company : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = null!;
    public string LegalName { get; private set; } = null!;
    public string DefaultCurrency { get; private set; } = "PKR";
    public string? TaxRegistrationNumber { get; private set; }
    public bool IsActive { get; private set; }

    private Company() { }

    public Company(Guid id, Guid tenantId, string name, string legalName, string defaultCurrency)
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
        LegalName = legalName;
        DefaultCurrency = defaultCurrency;
        IsActive = true;
    }

    public void Update(string name, string legalName, string defaultCurrency, string? taxRegistrationNumber)
    {
        Name = name;
        LegalName = legalName;
        DefaultCurrency = defaultCurrency;
        TaxRegistrationNumber = taxRegistrationNumber;
    }

    public void SetTaxRegistration(string? trn) => TaxRegistrationNumber = trn;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
