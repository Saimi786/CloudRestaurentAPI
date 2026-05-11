namespace CloudRestaurent.Modules.Pricing.Domain;

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
    All       = Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday
}

public static class DaysOfWeekFlagsExtensions
{
    public static DaysOfWeekFlags ToFlag(this DayOfWeek day) => day switch
    {
        DayOfWeek.Sunday    => DaysOfWeekFlags.Sunday,
        DayOfWeek.Monday    => DaysOfWeekFlags.Monday,
        DayOfWeek.Tuesday   => DaysOfWeekFlags.Tuesday,
        DayOfWeek.Wednesday => DaysOfWeekFlags.Wednesday,
        DayOfWeek.Thursday  => DaysOfWeekFlags.Thursday,
        DayOfWeek.Friday    => DaysOfWeekFlags.Friday,
        DayOfWeek.Saturday  => DaysOfWeekFlags.Saturday,
        _ => DaysOfWeekFlags.None
    };
}
