using System.Globalization;
using System.Windows.Input;
using DisplayToolkit.Automation.Engine;
using DisplayToolkit.Automation.Profiles;
using DisplayToolkit.Automation.Rules;
using DisplayToolkit.Automation.Storage;
using DisplayToolkit.Automation.Sun;

namespace DisplayToolkit.App.ViewModels.Automation;

/// <summary>The words for automation: status lines, rule sentences and shortcut names (design spec §4.2, §5.6).</summary>
internal static class AutomationText
{
    private static CultureInfo Culture => CultureInfo.CurrentCulture;

    /// <summary>The footer and day strip status, such as "Night since sunset, 18:21". Empty when there's nothing to say.</summary>
    public static string Status(AutomationEngine engine, MonitorAutomation automation)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(automation);
        var state = engine.State;
        var profile = state.ProfileId is { } id ? automation.FindProfile(id)?.Name : null;

        return state.Reason switch
        {
            AutomationReason.Paused => engine.PausedUntil is { } until
                ? $"Automation paused until {Time(until)}"
                : "Automation paused",
            AutomationReason.Manual when automation.Rules.Any(rule => rule.IsEnabled) =>
                $"Manual: automation resumes {Resumes(engine)}",
            AutomationReason.Manual => string.Empty,
            AutomationReason.Schedule when state.Rule?.Trigger is SunTrigger sun =>
                $"{profile} since {SunEventName(sun.Event)}, {Time(state.Since!.Value)}",
            AutomationReason.Schedule => $"{profile} since {Time(state.Since!.Value)}",
            AutomationReason.Condition => $"{profile} {While(state.Rule!.Trigger)}",
            _ => string.Empty,
        };
    }

    /// <summary>A rule as a sentence: "A game is running in full screen", "30 min before sunset", "Weekdays at 08:00".</summary>
    public static string Title(Trigger trigger) => trigger switch
    {
        AppTrigger app => $"{app.DisplayName} is open",
        FullscreenGameTrigger => "A game is running in full screen",
        PowerTrigger { Source: PowerSource.Battery } => "When on battery",
        PowerTrigger => "When plugged in",
        HdrTrigger { IsOn: true } => "When HDR is on",
        HdrTrigger => "When HDR is off",
        TimeTrigger time => $"{DaysName(time.Days)} at {Time(time.Time)}",
        SunTrigger sun => SunTitle(sun),
        _ => string.Empty,
    };

    /// <summary>The second line of a rule: when it next runs, or what ends it.</summary>
    public static string Description(Rule rule, GeoCoordinate? location)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (!rule.IsEnabled)
        {
            return "Turned off";
        }

        return rule.Trigger switch
        {
            AppTrigger app => $"Ends when {app.DisplayName} closes",
            FullscreenGameTrigger => "Ends when the game closes",
            PowerTrigger { Source: PowerSource.Battery } => "Ends when plugged in",
            PowerTrigger => "Ends when unplugged",
            HdrTrigger { IsOn: true } => "Ends when HDR turns off",
            HdrTrigger => "Ends when HDR turns on",
            SunTrigger when location is null => "Needs your location. Set it in Settings.",
            ScheduleTrigger schedule => NextOccurrence(schedule, location) is { } next ? Moment(next) : "Not in the next week",
            _ => string.Empty,
        };
    }

    /// <summary>The short trigger name under a profile in the profile list: "Sunset rule", "Ctrl + Alt + 4".</summary>
    public static string ProfileSubtitle(Profile profile, IEnumerable<Rule> rules)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (profile.Shortcut is { } shortcut)
        {
            return Shortcut(shortcut);
        }

        return rules.FirstOrDefault(rule => rule.IsEnabled && rule.ProfileId == profile.Id)?.Trigger switch
        {
            null => "No rule or shortcut",
            AppTrigger app => $"While {app.DisplayName} is open",
            FullscreenGameTrigger => "Full-screen games",
            PowerTrigger { Source: PowerSource.Battery } => "On battery",
            PowerTrigger => "Plugged in",
            HdrTrigger { IsOn: true } => "HDR on",
            HdrTrigger => "HDR off",
            TimeTrigger time => $"{Time(time.Time)} rule",
            SunTrigger { Event: SunEvent.Sunrise } => "Sunrise rule",
            SunTrigger => "Sunset rule",
            _ => string.Empty,
        };
    }

    /// <summary>"Ctrl + Alt + 4".</summary>
    public static string Shortcut(Shortcut shortcut)
    {
        ArgumentNullException.ThrowIfNull(shortcut);
        return string.Join(" + ", ShortcutKeys(shortcut));
    }

    /// <summary>The keys of a shortcut, each on its own keycap: ["Ctrl", "Alt", "4"].</summary>
    public static IReadOnlyList<string> ShortcutKeys(Shortcut shortcut)
    {
        ArgumentNullException.ThrowIfNull(shortcut);
        var keys = new List<string>();
        if (shortcut.Modifiers.HasFlag(ShortcutModifiers.Windows))
        {
            keys.Add("Win");
        }
        if (shortcut.Modifiers.HasFlag(ShortcutModifiers.Control))
        {
            keys.Add("Ctrl");
        }
        if (shortcut.Modifiers.HasFlag(ShortcutModifiers.Alt))
        {
            keys.Add("Alt");
        }
        if (shortcut.Modifiers.HasFlag(ShortcutModifiers.Shift))
        {
            keys.Add("Shift");
        }
        keys.Add(KeyName(KeyInterop.KeyFromVirtualKey(shortcut.VirtualKey)));
        return keys;
    }

    public static string KeyName(Key key) => key switch
    {
        >= Key.D0 and <= Key.D9 => ((int)(key - Key.D0)).ToString(Culture),
        >= Key.NumPad0 and <= Key.NumPad9 => $"Num {(int)(key - Key.NumPad0)}",
        Key.OemPlus => "+",
        Key.OemMinus => "-",
        Key.OemComma => ",",
        Key.OemPeriod => ".",
        Key.Prior => "Page Up",
        Key.Next => "Page Down",
        _ => key.ToString(),
    };

    public static string Time(TimeOnly time) => time.ToString("t", Culture);

    public static string Time(DateTimeOffset moment) => moment.ToLocalTime().ToString("t", Culture);

    /// <summary>"Today at 18:21", "Tomorrow at 07:14", "Monday at 08:00".</summary>
    public static string Moment(DateTimeOffset moment)
    {
        var local = moment.ToLocalTime();
        var days = (DateOnly.FromDateTime(local.DateTime).DayNumber - DateOnly.FromDateTime(DateTime.Now).DayNumber) switch
        {
            0 => "Today",
            1 => "Tomorrow",
            _ => Culture.DateTimeFormat.GetDayName(local.DayOfWeek),
        };
        return $"{days} at {Time(local)}";
    }

    public static string DaysName(Days days) => days switch
    {
        Days.Every => "Every day",
        Days.Weekdays => "Weekdays",
        Days.Weekends => "Weekends",
        _ => string.Join(", ", Enum.GetValues<DayOfWeek>()
            .OrderBy(day => ((int)day + 6) % 7) // Monday first
            .Where(day => days.Includes(day))
            .Select(day => Culture.DateTimeFormat.GetAbbreviatedDayName(day))),
    };

    private static string SunTitle(SunTrigger sun)
    {
        var name = SunEventName(sun.Event);
        var moment = sun.OffsetMinutes switch
        {
            0 => $"At {name}",
            < 0 => $"{Duration(-sun.OffsetMinutes)} before {name}",
            _ => $"{Duration(sun.OffsetMinutes)} after {name}",
        };
        return sun.Days == Days.Every ? moment : $"{moment}, {DaysName(sun.Days).ToLower(Culture)}";
    }

    private static string Duration(int minutes) => minutes switch
    {
        < 60 => $"{minutes} min",
        _ when minutes % 60 == 0 => $"{minutes / 60} h",
        _ => $"{minutes / 60} h {minutes % 60} min",
    };

    private static string SunEventName(SunEvent sunEvent) => sunEvent == SunEvent.Sunrise ? "sunrise" : "sunset";

    private static string While(Trigger trigger) => trigger switch
    {
        AppTrigger app => $"while {app.DisplayName} runs",
        FullscreenGameTrigger => "while a game runs",
        PowerTrigger { Source: PowerSource.Battery } => "while on battery",
        PowerTrigger => "while plugged in",
        HdrTrigger { IsOn: true } => "while HDR is on",
        HdrTrigger => "while HDR is off",
        _ => string.Empty,
    };

    /// <summary>"at sunrise", "at 07:00", or "when a rule applies" with only condition rules.</summary>
    private static string Resumes(AutomationEngine engine) => engine.NextScheduledEvent() switch
    {
        { Rule.Trigger: SunTrigger { OffsetMinutes: 0 } sun } => $"at {SunEventName(sun.Event)}",
        { } next => $"at {Time(next.At)}",
        null => "when a rule changes",
    };

    private static DateTimeOffset? NextOccurrence(ScheduleTrigger trigger, GeoCoordinate? location)
    {
        var now = DateTimeOffset.Now;
        var today = DateOnly.FromDateTime(now.LocalDateTime);
        for (var day = 0; day <= 7; day++)
        {
            if (trigger.OccurrenceOn(today.AddDays(day), TimeZoneInfo.Local, location) is { } at && at > now)
            {
                return at;
            }
        }
        return null;
    }
}
