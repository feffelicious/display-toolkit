using DisplayToolkit.Automation.Engine;
using DisplayToolkit.Automation.Storage;
using DisplayToolkit.Automation.Sun;

namespace DisplayToolkit.App.ViewModels.Automation;

/// <summary>A stretch of the day strip. Positions are fractions of the day (0 = midnight, 1 = the next midnight).</summary>
public sealed record DaySegment(double Start, double Length, string Name, string Glyph, bool IsActive);

/// <summary>A labelled moment above the strip, such as sunrise.</summary>
public sealed record DayMarker(double Position, string Glyph, string Time);

/// <summary>What the schedule does today, laid out for the day strip (design spec §5.6).</summary>
public sealed class DayStripViewModel
{
    public DayStripViewModel(AutomationEngine engine, MonitorAutomation automation, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(automation);
        var today = DateOnly.FromDateTime(now.LocalDateTime);
        var day = engine.GetDay(today);
        var start = day[0].Start;
        var length = (day[^1].End - start).TotalMinutes; // 23 or 25 hours on daylight saving days

        double Position(DateTimeOffset moment) => Math.Clamp((moment - start).TotalMinutes / length, 0, 1);

        Segments =
        [
            .. day.Select(segment =>
            {
                var profile = segment.ProfileId is { } id ? automation.FindProfile(id) : null;
                return new DaySegment(
                    Position(segment.Start),
                    Position(segment.End) - Position(segment.Start),
                    profile?.Name ?? "No profile",
                    profile?.Glyph ?? string.Empty,
                    profile is not null && profile.Id == engine.State.ProfileId);
            }),
        ];
        HasSchedule = day.Any(segment => segment.ProfileId is not null);
        Now = Position(now);

        if (engine.Location is { } location)
        {
            var sun = SunCalculator.Calculate(today, location, TimeZoneInfo.Local);
            Markers =
            [
                .. new[] { (sun.Sunrise, ""), (sun.Sunset, "") }
                    .Where(marker => marker.Item1 is not null)
                    .Select(marker => new DayMarker(Position(marker.Item1!.Value), marker.Item2, AutomationText.Time(marker.Item1.Value))),
            ];
        }
    }

    public IReadOnlyList<DaySegment> Segments { get; }

    public IReadOnlyList<DayMarker> Markers { get; } = [];

    /// <summary>Where "now" is on the strip.</summary>
    public double Now { get; }

    /// <summary>False when no schedule rule sets a profile today: the strip shows one empty stretch.</summary>
    public bool HasSchedule { get; }
}
