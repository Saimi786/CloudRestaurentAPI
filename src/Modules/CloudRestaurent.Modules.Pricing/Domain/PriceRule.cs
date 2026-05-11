using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Pricing.Domain;

/// <summary>
/// A rule that overrides a Product's BasePrice for a Branch (optional, null = all branches),
/// within an optional time-of-day window and an optional set of days of the week.
/// Higher Priority wins when multiple rules match. v1 = override the absolute price (Money);
/// percentage discounts come later.
/// </summary>
public class PriceRule : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; private set; }

    /// <summary>If null, the rule applies in any branch.</summary>
    public Guid? BranchId { get; private set; }

    public string Name { get; private set; } = null!;

    /// <summary>If both null, the rule applies all day. If both set, the rule applies in [Start, End).</summary>
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }

    /// <summary>Empty/None = applies every day.</summary>
    public DaysOfWeekFlags DaysOfWeek { get; private set; }

    public Money OverridePrice { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }

    private PriceRule() { }

    public PriceRule(
        Guid id, Guid tenantId, Guid productId, Guid? branchId, string name,
        TimeOnly? startTime, TimeOnly? endTime,
        DaysOfWeekFlags daysOfWeek, Money overridePrice, int priority)
    {
        ValidateTimes(startTime, endTime);

        Id = id;
        TenantId = tenantId;
        ProductId = productId;
        BranchId = branchId;
        Name = name;
        StartTime = startTime;
        EndTime = endTime;
        DaysOfWeek = daysOfWeek;
        OverridePrice = overridePrice;
        Priority = priority;
        IsActive = true;
    }

    public void Update(
        Guid? branchId, string name,
        TimeOnly? startTime, TimeOnly? endTime,
        DaysOfWeekFlags daysOfWeek, Money overridePrice, int priority)
    {
        ValidateTimes(startTime, endTime);
        BranchId = branchId;
        Name = name;
        StartTime = startTime;
        EndTime = endTime;
        DaysOfWeek = daysOfWeek;
        OverridePrice = overridePrice;
        Priority = priority;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    /// <summary>Does this rule apply at <paramref name="atLocal"/> for <paramref name="branchId"/>?</summary>
    public bool MatchesContext(Guid? branchId, DateTime atLocal)
    {
        if (!IsActive) return false;
        if (BranchId is not null && BranchId != branchId) return false;

        if (DaysOfWeek != DaysOfWeekFlags.None)
        {
            var todayFlag = atLocal.DayOfWeek.ToFlag();
            if ((DaysOfWeek & todayFlag) == 0) return false;
        }

        if (StartTime is { } st && EndTime is { } et)
        {
            var now = TimeOnly.FromDateTime(atLocal);
            // Handles crossing-midnight windows (e.g. 22:00–02:00).
            if (st < et)
            {
                if (now < st || now >= et) return false;
            }
            else // window wraps past midnight
            {
                if (now < st && now >= et) return false;
            }
        }

        return true;
    }

    private static void ValidateTimes(TimeOnly? start, TimeOnly? end)
    {
        if ((start is null) != (end is null))
            throw new ArgumentException("StartTime and EndTime must both be set or both null.");
    }
}
