using CloudRestaurent.Domain.Common;
using CloudRestaurent.Domain.Tenants;

namespace CloudRestaurent.Modules.SaaS.Domain;

public enum BillingInterval { Monthly = 0, Yearly = 1 }

/// <summary>
/// A subscription tier the platform sells. The feature set is encoded as flags on
/// <see cref="SubscriptionPlan"/> (Basic/Standard/Premium/Enterprise) so the existing
/// gate code keeps working — Package is the SKU + price + entitlements layer above it.
/// </summary>
public class Package : Entity<Guid>
{
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;       // "STANDARD-MONTHLY"
    public SubscriptionPlan Plan { get; private set; }
    public BillingInterval Interval { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = null!;
    public int MaxBranches { get; private set; }
    public int MaxUsers { get; private set; }
    public int? StorageGb { get; private set; }
    public string? FeatureNotes { get; private set; }
    public bool IsActive { get; private set; }

    private Package() { }

    public Package(
        Guid id, string code, string name, SubscriptionPlan plan, BillingInterval interval,
        decimal price, string currency, int maxBranches, int maxUsers, int? storageGb,
        string? featureNotes)
    {
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
        if (maxBranches <= 0) throw new ArgumentOutOfRangeException(nameof(maxBranches));
        if (maxUsers <= 0) throw new ArgumentOutOfRangeException(nameof(maxUsers));
        Id = id;
        Code = code;
        Name = name;
        Plan = plan;
        Interval = interval;
        Price = price;
        Currency = currency;
        MaxBranches = maxBranches;
        MaxUsers = maxUsers;
        StorageGb = storageGb;
        FeatureNotes = featureNotes;
        IsActive = true;
    }

    public void Update(string name, decimal price, int maxBranches, int maxUsers, int? storageGb, string? featureNotes)
    {
        Name = name; Price = price;
        MaxBranches = maxBranches; MaxUsers = maxUsers; StorageGb = storageGb;
        FeatureNotes = featureNotes;
    }
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
