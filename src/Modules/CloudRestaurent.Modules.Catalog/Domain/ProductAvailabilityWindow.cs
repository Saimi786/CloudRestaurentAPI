using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Catalog.Domain;

[Flags]
public enum DaysOfWeekFlags
{
    None      = 0,
    Sunday    = 1 << 0,
    Monday    = 1 << 1,
    Tuesday   = 1 << 2,
    Wednesday = 1 << 3,
    Thursday  = 1 << 4,
    Friday    = 1 << 5,
    Saturday  = 1 << 6,
    All       = 127
}

/// <summary>
/// "Day-part menu" rule: a product is on-menu only during the listed day-of-week + time
/// windows. A product with no windows is on-menu always (subject to <see cref="Product.IsAvailable"/>
/// for the manual 86 toggle). Multiple windows per product = unioned (e.g. lunch 11-3 AND dinner 6-10).
/// </summary>
public class ProductAvailabilityWindow : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = null!;          // "Lunch", "Happy Hour", etc.
    public DaysOfWeekFlags DaysOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public bool IsActive { get; private set; }

    private ProductAvailabilityWindow() { }

    public ProductAvailabilityWindow(
        Guid id, Guid tenantId, Guid productId, string name,
        DaysOfWeekFlags days, TimeOnly start, TimeOnly end)
    {
        if (days == DaysOfWeekFlags.None)
            throw new ArgumentException("At least one day must be selected.", nameof(days));
        Id = id;
        TenantId = tenantId;
        ProductId = productId;
        Name = name;
        DaysOfWeek = days;
        StartTime = start;
        EndTime = end;
        IsActive = true;
    }

    public void Update(string name, DaysOfWeekFlags days, TimeOnly start, TimeOnly end)
    {
        if (days == DaysOfWeekFlags.None)
            throw new ArgumentException("At least one day must be selected.", nameof(days));
        Name = name;
        DaysOfWeek = days;
        StartTime = start;
        EndTime = end;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// True if this window covers the given moment. Handles wrap-past-midnight (22:00→02:00).
    /// </summary>
    public bool Covers(DateTime now)
    {
        if (!IsActive) return false;
        var bit = (DaysOfWeekFlags)(1 << (int)now.DayOfWeek);
        if ((DaysOfWeek & bit) == 0) return false;
        var t = TimeOnly.FromDateTime(now);
        if (StartTime <= EndTime) return t >= StartTime && t <= EndTime;
        // Wrap: e.g. 22:00 → 02:00 — covered if t >= start OR t <= end
        return t >= StartTime || t <= EndTime;
    }
}
