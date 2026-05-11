using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Domain.Tenants;

public class Tenant : AuditableEntity<Guid>
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public BusinessType BusinessType { get; private set; }
    public SubscriptionPlan Plan { get; private set; }
    public bool IsActive { get; private set; }
    public string? LogoUrl { get; private set; }

    private Tenant() { }

    public Tenant(Guid id, string name, string slug, BusinessType businessType, SubscriptionPlan plan)
    {
        Id = id;
        Name = name;
        Slug = slug;
        BusinessType = businessType;
        Plan = plan;
        IsActive = true;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    public void ChangePlan(SubscriptionPlan plan) => Plan = plan;
    public void SetLogoUrl(string? url) => LogoUrl = url;

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name is required.", nameof(name));
        Name = name;
    }
}

public enum SubscriptionPlan
{
    Basic = 0,
    Standard = 1,
    Premium = 2,
    Enterprise = 3
}
