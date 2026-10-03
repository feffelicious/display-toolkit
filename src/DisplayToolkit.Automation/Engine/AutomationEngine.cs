using DisplayToolkit.Automation.Rules;
using DisplayToolkit.Automation.Sun;

namespace DisplayToolkit.Automation.Engine;

/// <summary>
/// Decides which profile should be active (design spec §5.6). Pure logic over a <see cref="TimeProvider"/>: the host
/// reports condition changes and calls <see cref="Tick"/> at <see cref="NextWakeUp"/>; nothing here touches a monitor.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Schedule rules are events. The <em>baseline</em> is the profile of the most recent one that fired (looking
/// back a week).</item>
/// <item>Condition rules are states. The first active one in list order wins over the baseline.</item>
/// <item>A manual choice holds until the next rule event: a schedule rule fires or a condition starts or ends.</item>
/// <item>While paused, nothing is chosen.</item>
/// </list>
/// Not thread-safe: call it from one thread.
/// </remarks>
public sealed class AutomationEngine
{
    private const int LookbackDays = 7;

    private readonly TimeProvider _time;
    private readonly Dictionary<Guid, DateTimeOffset> _activeConditions = [];
    private IReadOnlyList<Rule> _rules = [];
    private HashSet<Guid> _profileIds = [];
    private (Guid? ProfileId, DateTimeOffset Since)? _manual;
    private DateTimeOffset _lastTick;

    public AutomationEngine(TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        _time = time;
        _lastTick = Now;
    }

    public event EventHandler? StateChanged;

    public AutomationState State { get; private set; } = AutomationState.Idle;

    public bool IsPaused { get; private set; }

    /// <summary>When a pause ends by itself; null while paused until resumed, or not paused.</summary>
    public DateTimeOffset? PausedUntil { get; private set; }

    /// <summary>The location sun rules use, if known.</summary>
    public GeoCoordinate? Location { get; private set; }

    /// <summary>The next moment <see cref="Tick"/> must be called: a schedule rule fires or a pause ends.</summary>
    public DateTimeOffset? NextWakeUp
    {
        get
        {
            var next = NextScheduledEvent()?.At;
            return PausedUntil is { } until && (next is null || until < next) ? until : next;
        }
    }

    private DateTimeOffset Now => _time.GetLocalNow();

    private TimeZoneInfo Zone => _time.LocalTimeZone;

    /// <summary>Rules that can apply: turned on, and their profile still exists.</summary>
    private IEnumerable<Rule> LiveRules => _rules.Where(rule => rule.IsEnabled && _profileIds.Contains(rule.ProfileId));

    /// <summary>Replaces the rules (in priority order), the profiles they may refer to, and the location for sun rules.</summary>
    public void Configure(IReadOnlyList<Rule> rules, IEnumerable<Guid> profileIds, GeoCoordinate? location)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(profileIds);
        _rules = rules;
        _profileIds = [.. profileIds];
        Location = location;

        var ruleIds = rules.Select(rule => rule.Id).ToHashSet();
        foreach (var gone in _activeConditions.Keys.Where(id => !ruleIds.Contains(id)).ToList())
        {
            _activeConditions.Remove(gone);
        }
        Evaluate();
    }

    /// <summary>Reports which condition rules currently hold. Any change is a rule event and ends a manual choice.</summary>
    public void SetActiveConditions(IEnumerable<Guid> ruleIds)
    {
        ArgumentNullException.ThrowIfNull(ruleIds);
        var active = ruleIds.ToHashSet();
        var changed = false;

        foreach (var ended in _activeConditions.Keys.Where(id => !active.Contains(id)).ToList())
        {
            _activeConditions.Remove(ended);
            changed = true;
        }
        foreach (var started in active.Where(id => !_activeConditions.ContainsKey(id)))
        {
            _activeConditions[started] = Now;
            changed = true;
        }

        if (changed)
        {
            _manual = null;
        }
        Evaluate();
    }

    /// <summary>Lets time pass: fires schedule rules that came due and ends an expired pause.</summary>
    public void Tick()
    {
        var now = Now;
        if (PausedUntil is { } until && until <= now)
        {
            IsPaused = false;
            PausedUntil = null;
            _manual = null;
        }
        if (Occurrences(_lastTick, now).Any(occurrence => occurrence.At > _lastTick))
        {
            _manual = null;
        }
        _lastTick = now;
        Evaluate();
    }

    /// <summary>
    /// The user picked a profile, or changed a setting (then pass the profile that stays highlighted, if any).
    /// Automation leaves things alone until the next rule event.
    /// </summary>
    public void SetManual(Guid? profileId)
    {
        _manual = (profileId, Now);
        Evaluate();
    }

    /// <summary>Stops choosing profiles until <paramref name="until"/>, or until <see cref="Resume"/> when null.</summary>
    public void Pause(DateTimeOffset? until)
    {
        IsPaused = true;
        PausedUntil = until;
        Evaluate();
    }

    /// <summary>Ends a pause or a manual choice and lets the rules decide again.</summary>
    public void Resume()
    {
        IsPaused = false;
        PausedUntil = null;
        _manual = null;
        Evaluate();
    }

    /// <summary>The next schedule rule to fire within a week, if any.</summary>
    public ScheduledEvent? NextScheduledEvent()
    {
        var now = Now;
        return Occurrences(now, now.AddDays(LookbackDays + 1)).FirstOrDefault(occurrence => occurrence.At > now);
    }

    /// <summary>What the schedule does over one local day, for the day strip. Condition rules aren't included.</summary>
    public IReadOnlyList<ScheduleSegment> GetDay(DateOnly date)
    {
        var start = StartOf(date);
        var end = StartOf(date.AddDays(1));
        var segments = new List<ScheduleSegment>();

        var current = LastOccurrence(start);
        var segmentStart = start;
        foreach (var occurrence in Occurrences(start, end).Where(occurrence => occurrence.At > start && occurrence.At < end))
        {
            if (occurrence.At > segmentStart)
            {
                segments.Add(new ScheduleSegment(segmentStart, occurrence.At, current?.Rule.ProfileId, current?.Rule));
            }
            segmentStart = occurrence.At;
            current = occurrence;
        }
        segments.Add(new ScheduleSegment(segmentStart, end, current?.Rule.ProfileId, current?.Rule));
        return segments;
    }

    private void Evaluate()
    {
        var state = Compute();
        if (state != State)
        {
            State = state;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private AutomationState Compute()
    {
        if (IsPaused)
        {
            return new AutomationState(null, AutomationReason.Paused);
        }
        if (_manual is var (profileId, since))
        {
            return new AutomationState(profileId, AutomationReason.Manual, Since: since);
        }

        foreach (var rule in LiveRules.Where(rule => rule.Trigger is not ScheduleTrigger))
        {
            if (_activeConditions.TryGetValue(rule.Id, out var started))
            {
                return new AutomationState(rule.ProfileId, AutomationReason.Condition, rule, started);
            }
        }

        return LastOccurrence(Now) is { } last
            ? new AutomationState(last.Rule.ProfileId, AutomationReason.Schedule, last.Rule, last.At)
            : AutomationState.Idle;
    }

    /// <summary>The most recent schedule event at or before <paramref name="moment"/>, looking back a week.</summary>
    private ScheduledEvent? LastOccurrence(DateTimeOffset moment) =>
        Occurrences(moment.AddDays(-LookbackDays), moment).LastOrDefault();

    /// <summary>
    /// Schedule events between <paramref name="from"/> and <paramref name="to"/> (inclusive), in time order. When two
    /// rules fire at the same moment, the higher one comes last so that it wins.
    /// </summary>
    private List<ScheduledEvent> Occurrences(DateTimeOffset from, DateTimeOffset to)
    {
        var rules = LiveRules.Where(rule => rule.Trigger is ScheduleTrigger).Select((rule, priority) => (Rule: rule, Priority: priority)).ToList();
        if (rules.Count == 0)
        {
            return [];
        }

        // One day of margin either side: the local date of an event can differ from the date of the bounds.
        var firstDay = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(from, Zone).DateTime).AddDays(-1);
        var lastDay = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(to, Zone).DateTime).AddDays(1);

        var occurrences = new List<(ScheduledEvent Event, int Priority)>();
        for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
        {
            foreach (var (rule, priority) in rules)
            {
                if (((ScheduleTrigger)rule.Trigger).OccurrenceOn(day, Zone, Location) is { } at && at >= from && at <= to)
                {
                    occurrences.Add((new ScheduledEvent(rule, at), priority));
                }
            }
        }
        return
        [
            .. occurrences
                .OrderBy(occurrence => occurrence.Event.At)
                .ThenByDescending(occurrence => occurrence.Priority)
                .Select(occurrence => occurrence.Event),
        ];
    }

    private DateTimeOffset StartOf(DateOnly date)
    {
        var midnight = date.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(midnight, Zone.GetUtcOffset(midnight));
    }
}
