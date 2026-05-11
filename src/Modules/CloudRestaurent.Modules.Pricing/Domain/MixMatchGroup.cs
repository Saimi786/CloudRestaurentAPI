using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Pricing.Domain;

/// <summary>
/// Group-based promotion: "buy N items from this group → get discount/price". Time-,
/// day-, and date-window-aware so happy hour and weekend specials Just Work.
/// </summary>
public class MixMatchGroup : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = null!;
    public MixMatchType Type { get; private set; }
    public int Quantity { get; private set; }
    public decimal DiscountValue { get; private set; }

    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public DaysOfWeekFlags DaysOfWeek { get; private set; }
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }

    public int Priority { get; private set; }

    /// <summary>
    /// True (default) → applies if qualifying, additive with other groups.
    /// False → exclusive: only the highest-discount non-stackable group applies (others ignored).
    /// Stackable groups always combine with the winning non-stackable.
    /// </summary>
    public bool Stackable { get; private set; } = true;

    public bool IsActive { get; private set; }

    private readonly List<MixMatchProduct> _products = new();
    public IReadOnlyCollection<MixMatchProduct> Products => _products;

    private MixMatchGroup() { }

    public MixMatchGroup(
        Guid id, Guid tenantId,
        string name, MixMatchType type, int quantity, decimal discountValue)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be > 0.");
        if (discountValue < 0) throw new ArgumentOutOfRangeException(nameof(discountValue));
        if (type == MixMatchType.PercentDiscount && discountValue > 100)
            throw new ArgumentOutOfRangeException(nameof(discountValue), "Percent discount must be 0-100.");

        Id = id;
        TenantId = tenantId;
        Name = name;
        Type = type;
        Quantity = quantity;
        DiscountValue = discountValue;
        DaysOfWeek = DaysOfWeekFlags.All;   // no day restriction by default
        Stackable = true;
        IsActive = true;
    }

    public void SetStackable(bool stackable) => Stackable = stackable;

    public void Update(
        string name, MixMatchType type, int quantity, decimal discountValue, int priority)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (discountValue < 0) throw new ArgumentOutOfRangeException(nameof(discountValue));
        if (type == MixMatchType.PercentDiscount && discountValue > 100)
            throw new ArgumentOutOfRangeException(nameof(discountValue), "Percent discount must be 0-100.");

        Name = name;
        Type = type;
        Quantity = quantity;
        DiscountValue = discountValue;
        Priority = priority;
    }

    public void SetDateWindow(DateOnly? startDate, DateOnly? endDate)
    {
        if (startDate.HasValue && endDate.HasValue && endDate < startDate)
            throw new ArgumentException("End date must be on or after start date.");
        StartDate = startDate;
        EndDate = endDate;
    }

    public void SetTimeWindow(TimeOnly? startTime, TimeOnly? endTime)
    {
        // Allow inverse windows (e.g. 22:00→02:00) — caller interprets via "wrap past midnight".
        StartTime = startTime;
        EndTime = endTime;
    }

    public void SetDaysOfWeek(DaysOfWeekFlags days) => DaysOfWeek = days;

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public void ReplaceProducts(IEnumerable<Guid> productIds)
    {
        _products.Clear();
        foreach (var pid in productIds.Distinct())
            _products.Add(new MixMatchProduct(Guid.NewGuid(), Id, pid));
    }
}

public class MixMatchProduct : Entity<Guid>
{
    public Guid MixMatchGroupId { get; private set; }
    public Guid ProductId { get; private set; }

    private MixMatchProduct() { }

    public MixMatchProduct(Guid id, Guid groupId, Guid productId)
    {
        Id = id;
        MixMatchGroupId = groupId;
        ProductId = productId;
    }
}
