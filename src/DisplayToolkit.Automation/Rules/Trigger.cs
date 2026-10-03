using System.Text.Json.Serialization;
using DisplayToolkit.Automation.Sun;

namespace DisplayToolkit.Automation.Rules;

/// <summary>
/// What makes a rule apply. Schedule triggers (<see cref="ScheduleTrigger"/>) are moments: they set the profile when
/// they fire, and it stays until something else changes it. Condition triggers are states: the rule applies while the
/// condition holds.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AppTrigger), "app")]
[JsonDerivedType(typeof(FullscreenGameTrigger), "fullscreen-game")]
[JsonDerivedType(typeof(PowerTrigger), "power")]
[JsonDerivedType(typeof(HdrTrigger), "hdr")]
[JsonDerivedType(typeof(TimeTrigger), "time")]
[JsonDerivedType(typeof(SunTrigger), "sun")]
public abstract record Trigger;

/// <summary>An app is running. <see cref="ProcessName"/> is the executable name without ".exe", matched case-insensitively.</summary>
public sealed record AppTrigger(string ProcessName, string DisplayName) : Trigger;

/// <summary>A game (or another full-screen app that isn't a browser or video player) fills a screen.</summary>
public sealed record FullscreenGameTrigger : Trigger;

public enum PowerSource
{
    PluggedIn,
    Battery,
}

public sealed record PowerTrigger(PowerSource Source) : Trigger;

/// <summary>Windows HDR is on (or, with <see cref="IsOn"/> false, off) for the monitor.</summary>
public sealed record HdrTrigger(bool IsOn) : Trigger;

/// <summary>A trigger that fires at moments in time.</summary>
public abstract record ScheduleTrigger(Days Days) : Trigger
{
    /// <summary>When the trigger fires on <paramref name="date"/> (local), if it does.</summary>
    public abstract DateTimeOffset? OccurrenceOn(DateOnly date, TimeZoneInfo zone, GeoCoordinate? location);
}

public sealed record TimeTrigger(TimeOnly Time, Days Days) : ScheduleTrigger(Days)
{
    public override DateTimeOffset? OccurrenceOn(DateOnly date, TimeZoneInfo zone, GeoCoordinate? location)
    {
        ArgumentNullException.ThrowIfNull(zone);
        if (!Days.Includes(date.DayOfWeek))
        {
            return null;
        }

        var local = date.ToDateTime(Time);
        // A time skipped by a daylight saving change happens an hour later that day.
        if (zone.IsInvalidTime(local))
        {
            local = local.AddHours(1);
        }
        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }
}

public enum SunEvent
{
    Sunrise,
    Sunset,
}

/// <summary>Sunrise or sunset at the user's location, shifted by <see cref="OffsetMinutes"/>.</summary>
public sealed record SunTrigger(SunEvent Event, int OffsetMinutes, Days Days) : ScheduleTrigger(Days)
{
    public override DateTimeOffset? OccurrenceOn(DateOnly date, TimeZoneInfo zone, GeoCoordinate? location)
    {
        ArgumentNullException.ThrowIfNull(zone);
        if (location is null || !Days.Includes(date.DayOfWeek))
        {
            return null;
        }

        var times = SunCalculator.Calculate(date, location, zone);
        var time = Event == SunEvent.Sunrise ? times.Sunrise : times.Sunset;
        return time?.AddMinutes(OffsetMinutes);
    }
}
