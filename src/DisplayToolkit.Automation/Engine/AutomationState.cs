using DisplayToolkit.Automation.Rules;

namespace DisplayToolkit.Automation.Engine;

public enum AutomationReason
{
    /// <summary>No rule applies: settings stay as they are.</summary>
    None,

    /// <summary>The profile of the most recent schedule rule (the baseline).</summary>
    Schedule,

    /// <summary>A condition rule applies, such as a running app.</summary>
    Condition,

    /// <summary>The user picked a profile or changed a setting; automation waits for the next rule event.</summary>
    Manual,

    /// <summary>The user paused automation.</summary>
    Paused,
}

/// <summary>The profile automation wants right now, and why.</summary>
/// <param name="ProfileId">The profile to use. Null means "leave the settings alone".</param>
/// <param name="Rule">The rule that chose the profile (schedule and condition reasons).</param>
/// <param name="Since">When the schedule rule fired, the condition started, or the manual choice was made.</param>
public sealed record AutomationState(Guid? ProfileId, AutomationReason Reason, Rule? Rule = null, DateTimeOffset? Since = null)
{
    public static AutomationState Idle { get; } = new(null, AutomationReason.None);
}

/// <summary>A schedule rule firing at a moment.</summary>
public sealed record ScheduledEvent(Rule Rule, DateTimeOffset At);

/// <summary>A stretch of a day during which the schedule baseline is one profile (for the day strip).</summary>
public sealed record ScheduleSegment(DateTimeOffset Start, DateTimeOffset End, Guid? ProfileId, Rule? Rule);
