namespace DisplayToolkit.Automation.Rules;

/// <summary>Days of the week a schedule rule runs on.</summary>
[Flags]
public enum Days
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64,
    Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
    Weekends = Saturday | Sunday,
    Every = Weekdays | Weekends,
}

public static class DaysExtensions
{
    public static bool Includes(this Days days, DayOfWeek day) => (days & Of(day)) != 0;

    public static Days Of(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Days.Monday,
        DayOfWeek.Tuesday => Days.Tuesday,
        DayOfWeek.Wednesday => Days.Wednesday,
        DayOfWeek.Thursday => Days.Thursday,
        DayOfWeek.Friday => Days.Friday,
        DayOfWeek.Saturday => Days.Saturday,
        _ => Days.Sunday,
    };
}
