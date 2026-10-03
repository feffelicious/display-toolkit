using System.Windows.Input;
using System.Windows.Threading;
using DisplayToolkit.App.ViewModels;
using DisplayToolkit.Automation.Conditions;
using DisplayToolkit.Automation.Engine;
using DisplayToolkit.Automation.Profiles;
using DisplayToolkit.Automation.Rules;
using DisplayToolkit.Automation.Storage;
using DisplayToolkit.Core.Monitors;
using Microsoft.Extensions.Logging;

namespace DisplayToolkit.App.Services;

/// <summary>
/// Runs automation for the monitor in <see cref="MonitorContext"/>: keeps its profiles and rules, feeds the
/// <see cref="AutomationEngine"/> the time and this PC's state, applies the profile it chooses, and owns the profile
/// shortcuts. Lives on the UI thread; all events are raised there.
/// </summary>
internal sealed class AutomationService : IDisposable
{
    /// <summary>How often running apps, full-screen games, power and HDR are checked while condition rules exist.</summary>
    private static readonly TimeSpan ConditionPollInterval = TimeSpan.FromSeconds(2);

    /// <summary>The clock is checked at least this often, so sleep, resume and clock changes are noticed.</summary>
    private static readonly TimeSpan MaximumWakeInterval = TimeSpan.FromMinutes(1);

    /// <summary>The location is asked for again after this long, for laptops that travel.</summary>
    private static readonly TimeSpan LocationRefreshInterval = TimeSpan.FromHours(6);

    private const int FirstProfileHotkeyId = 100;

    private readonly MonitorContext _context;
    private readonly AutomationStore _store;
    private readonly LocationService _location;
    private readonly GlobalHotkeys _hotkeys;
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<AutomationService> _logger;
    private readonly AutomationEngine _engine = new(TimeProvider.System);
    private readonly DispatcherTimer _wakeTimer = new();
    private readonly DispatcherTimer _conditionTimer = new() { Interval = ConditionPollInterval };
    private readonly DispatcherTimer _locationTimer = new() { Interval = LocationRefreshInterval };
    private readonly SemaphoreSlim _applyGate = new(1, 1);
    private readonly Dictionary<int, Guid> _hotkeyProfiles = [];
    private readonly HashSet<Guid> _shortcutConflicts = [];

    private MonitorViewModel? _monitor;
    private bool _isApplying;
    private bool _isSampling;

    /// <summary>The profile written last, by automation or by the user. Automation doesn't write it again.</summary>
    private Guid? _appliedProfileId;

    /// <summary>
    /// The values automation replaced, captured before it first applied a profile. Put back when no rule applies any
    /// more (the app closed and there's no schedule), so a rule like "while VLC is open" undoes itself.
    /// </summary>
    private Dictionary<string, uint>? _restorePoint;

    public AutomationService(MonitorContext context, AutomationStore store, LocationService location, GlobalHotkeys hotkeys, Dispatcher dispatcher,
        ILogger<AutomationService> logger)
    {
        _dispatcher = dispatcher;
        _context = context;
        _store = store;
        _location = location;
        _hotkeys = hotkeys;
        _logger = logger;
        _engine.StateChanged += (_, _) => OnStateChanged();
        _wakeTimer.Tick += (_, _) => Tick();
        _conditionTimer.Tick += async (_, _) => await SampleConditionsAsync();
        _locationTimer.Tick += async (_, _) => await _location.RefreshAsync();
        _hotkeys.Pressed += (_, id) => OnHotkey(id);
        _location.Changed += (_, _) => Configure();
    }

    /// <summary>Profiles, rules, the state or the applying status changed.</summary>
    public event EventHandler? Changed;

    /// <summary>A profile was switched with its shortcut (for the on-screen overlay).</summary>
    public event EventHandler<Profile>? ShortcutUsed;

    public MonitorAutomation Automation { get; private set; } = MonitorAutomation.Empty;

    public IReadOnlyList<Profile> Profiles => Automation.Profiles;

    public IReadOnlyList<Rule> Rules => Automation.Rules;

    public AutomationEngine Engine => _engine;

    public AutomationState State => _engine.State;

    /// <summary>The profile in use: chosen by a rule, or picked by the user.</summary>
    public Profile? ActiveProfile => State.ProfileId is { } id ? Automation.FindProfile(id) : null;

    public MonitorViewModel? Monitor => _monitor;

    /// <summary>A profile is being written to the monitor.</summary>
    public bool IsApplying { get; private set; }

    /// <summary>The last application that didn't fully work, until the next one or until dismissed.</summary>
    public ProfileApplyResult? LastFailure { get; private set; }

    /// <summary>Profiles whose shortcut is taken by Windows or another app.</summary>
    public IReadOnlySet<Guid> ShortcutConflicts => _shortcutConflicts;

    public void Start()
    {
        _context.Changed += (_, _) => OnMonitorChanged();
        OnMonitorChanged();
        _locationTimer.Start();
        _ = _location.RefreshAsync();
    }

    /// <summary>The profile settings this monitor has.</summary>
    public static IEnumerable<ProfileSetting> SettingsFor(MonitorViewModel monitor) =>
        ProfileSettings.All.Where(setting => setting.Feature is null ? monitor.WindowsHdr is not null : monitor.Session.Supports(setting.Feature));

    // ------------------------------------------------------------------ Editing

    /// <summary>Adds the profile, or replaces the one with the same id.</summary>
    public void SaveProfile(Profile profile) => Update(automation => automation with
    {
        Profiles = automation.FindProfile(profile.Id) is null
            ? [.. automation.Profiles, profile]
            : [.. automation.Profiles.Select(existing => existing.Id == profile.Id ? profile : existing)],
    });

    /// <summary>Deletes a profile and the rules that use it.</summary>
    public void DeleteProfile(Guid id) => Update(automation => automation with
    {
        Profiles = [.. automation.Profiles.Where(profile => profile.Id != id)],
        Rules = [.. automation.Rules.Where(rule => rule.ProfileId != id)],
    });

    /// <summary>Adds the rule at the end (lowest priority), or replaces the one with the same id.</summary>
    public void SaveRule(Rule rule) => Update(automation => automation with
    {
        Rules = automation.Rules.Any(existing => existing.Id == rule.Id)
            ? [.. automation.Rules.Select(existing => existing.Id == rule.Id ? rule : existing)]
            : [.. automation.Rules, rule],
    });

    public void DeleteRule(Guid id) => Update(automation => automation with
    {
        Rules = [.. automation.Rules.Where(rule => rule.Id != id)],
    });

    /// <summary>Moves a rule to <paramref name="index"/> in the priority order.</summary>
    public void MoveRule(Guid id, int index) => Update(automation =>
    {
        var rules = automation.Rules.ToList();
        var rule = rules.Find(rule => rule.Id == id);
        if (rule is null)
        {
            return automation;
        }
        rules.Remove(rule);
        rules.Insert(Math.Clamp(index, 0, rules.Count), rule);
        return automation with { Rules = rules };
    });

    /// <summary>The current values of the given settings on the monitor.</summary>
    public Dictionary<string, uint> CaptureCurrent(IEnumerable<string> settingIds) =>
        _monitor is { } monitor ? MonitorProfileTarget.Capture(monitor, settingIds) : [];

    // ------------------------------------------------------------------ Using

    /// <summary>The user picked a profile: apply it, and let automation wait for the next rule event.</summary>
    public async Task ApplyAsync(Profile profile)
    {
        _restorePoint = null;
        _engine.SetManual(profile.Id);
        await ApplyProfileAsync(profile, byAutomation: false);
    }

    /// <summary>Pauses automation for <paramref name="duration"/>, or until <see cref="Resume"/> when null.</summary>
    public void Pause(TimeSpan? duration)
    {
        _engine.Pause(duration is { } value ? DateTimeOffset.Now + value : null);
        ScheduleWake();
    }

    /// <summary>Pauses until 06:00 tomorrow (or today, if it isn't 06:00 yet).</summary>
    public void PauseUntilMorning()
    {
        var now = DateTimeOffset.Now;
        var morning = new DateTimeOffset(now.Date.AddHours(6), now.Offset);
        _engine.Pause(morning > now ? morning : morning.AddDays(1));
        ScheduleWake();
    }

    public void Resume() => _engine.Resume();

    /// <summary>Applies the profile from <see cref="LastFailure"/> again.</summary>
    public Task RetryAsync() =>
        LastFailure is { } failure ? ApplyProfileAsync(failure.Profile, byAutomation: false) : Task.CompletedTask;

    public void DismissFailure()
    {
        LastFailure = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _wakeTimer.Stop();
        _conditionTimer.Stop();
        _locationTimer.Stop();
        DetachMonitor();
        _applyGate.Dispose();
    }

    // ------------------------------------------------------------------ Internals

    private void Update(Func<MonitorAutomation, MonitorAutomation> change)
    {
        if (_monitor is not { } monitor)
        {
            return;
        }
        Automation = change(Automation);
        _store.Set(monitor.Session.Id.Model, Automation);
        RegisterShortcuts();
        Configure();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnMonitorChanged()
    {
        if (_monitor == _context.Current)
        {
            return;
        }

        DetachMonitor();
        _monitor = _context.Current;
        _appliedProfileId = null;
        _restorePoint = null;
        Automation = _monitor is { } monitor ? _store.Get(monitor.Session.Id.Model) : MonitorAutomation.Empty;
        if (_monitor is not null)
        {
            _monitor.Session.ValueChanged += OnSessionValueChanged;
        }

        RegisterShortcuts();
        Configure();
        // Configure only reports a change of state; a new monitor needs the current choice applied either way.
        OnStateChanged();
    }

    private void DetachMonitor()
    {
        if (_monitor is not null)
        {
            _monitor.Session.ValueChanged -= OnSessionValueChanged;
        }
    }

    private void Configure()
    {
        _engine.Configure(Automation.Rules, Automation.Profiles.Select(profile => profile.Id), _location.Current);
        var hasConditions = Automation.Rules.Any(rule => rule.IsEnabled && rule.Trigger is not ScheduleTrigger);
        if (hasConditions)
        {
            _conditionTimer.Start();
        }
        else
        {
            _conditionTimer.Stop();
            _engine.SetActiveConditions([]);
        }
        ScheduleWake();
    }

    private void Tick()
    {
        _engine.Tick();
        ScheduleWake();
        Changed?.Invoke(this, EventArgs.Empty); // Relative times in the status ("since 18:21") stay current.
    }

    private void ScheduleWake()
    {
        _wakeTimer.Stop();
        var delay = MaximumWakeInterval;
        if (_engine.NextWakeUp is { } next && next - DateTimeOffset.Now < delay)
        {
            delay = next - DateTimeOffset.Now;
        }
        _wakeTimer.Interval = delay > TimeSpan.Zero ? delay + TimeSpan.FromMilliseconds(50) : TimeSpan.FromMilliseconds(50);
        _wakeTimer.Start();
    }

    private async Task SampleConditionsAsync()
    {
        if (_isSampling || _monitor is not { } monitor)
        {
            return;
        }

        _isSampling = true;
        try
        {
            var id = monitor.Session.Id;
            var snapshot = await Task.Run(() => SystemSampler.Capture(WindowsHdr.GetState(id)?.IsEnabled == true));
            _engine.SetActiveConditions(ConditionEvaluator.ActiveRules(Automation.Rules, snapshot));
        }
        finally
        {
            _isSampling = false;
        }
    }

    private void OnStateChanged()
    {
        var state = _engine.State;
        if (state.Reason is AutomationReason.Schedule or AutomationReason.Condition
            && state.ProfileId != _appliedProfileId
            && Automation.FindProfile(state.ProfileId!.Value) is { } profile)
        {
            _ = ApplyProfileAsync(profile, byAutomation: true);
        }
        else if (state.Reason == AutomationReason.None && _restorePoint is { } restore)
        {
            _restorePoint = null;
            _appliedProfileId = null;
            _ = ApplyProfileAsync(new Profile { Id = Guid.Empty, Name = "Previous settings", Settings = restore }, byAutomation: false);
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private async Task ApplyProfileAsync(Profile profile, bool byAutomation)
    {
        if (_monitor is not { } monitor)
        {
            return;
        }

        _appliedProfileId = profile.Id == Guid.Empty ? null : profile.Id;
        await _applyGate.WaitAsync();
        try
        {
            if (byAutomation)
            {
                // Remember what automation is about to change, the first time it changes each setting.
                _restorePoint ??= [];
                foreach (var (id, value) in MonitorProfileTarget.Capture(monitor, profile.Settings.Keys.Where(id => !_restorePoint.ContainsKey(id))))
                {
                    _restorePoint[id] = value;
                }
            }

            _isApplying = true;
            IsApplying = true;
            Changed?.Invoke(this, EventArgs.Empty);
            _logger.LogInformation("Applying {Profile} ({Reason})", profile.Name, byAutomation ? State.Reason : "picked");
            var result = await ProfileApplier.ApplyAsync(profile, new MonitorProfileTarget(monitor));
            LastFailure = result.Succeeded ? null : result;
            if (!result.Succeeded)
            {
                _logger.LogWarning("{Profile} applied, except {Settings}", profile.Name, string.Join(", ", result.Failed.Select(setting => setting.Name)));
            }
        }
        finally
        {
            _isApplying = false;
            IsApplying = false;
            _applyGate.Release();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// The user changed a setting that the active profile includes: that's a manual choice, so automation stops
    /// enforcing the profile until the next rule event. Raised on the DDC thread for our own writes too; those are
    /// recognized by <see cref="_isApplying"/>, which is read before switching threads.
    /// </summary>
    private void OnSessionValueChanged(object? sender, FeatureValueChangedEventArgs e)
    {
        if (e.Value.Status != FeatureStatus.Pending || _isApplying)
        {
            return;
        }
        var featureId = e.Value.Feature.Id;
        _dispatcher.BeginInvoke(() =>
        {
            if (State.Reason is AutomationReason.Schedule or AutomationReason.Condition
                && ActiveProfile is { } profile && profile.Settings.ContainsKey(featureId))
            {
                _restorePoint = null;
                _engine.SetManual(profile.Id);
            }
        });
    }

    private void RegisterShortcuts()
    {
        foreach (var id in _hotkeyProfiles.Keys)
        {
            _hotkeys.Unregister(id);
        }
        _hotkeyProfiles.Clear();
        _shortcutConflicts.Clear();

        var nextId = FirstProfileHotkeyId;
        foreach (var profile in Automation.Profiles.Where(profile => profile.Shortcut is not null))
        {
            var shortcut = profile.Shortcut!;
            var id = nextId++;
            if (_hotkeys.Register(id, (ModifierKeys)shortcut.Modifiers, KeyInterop.KeyFromVirtualKey(shortcut.VirtualKey)))
            {
                _hotkeyProfiles[id] = profile.Id;
            }
            else
            {
                _shortcutConflicts.Add(profile.Id);
                _logger.LogWarning("The shortcut for {Profile} is taken", profile.Name);
            }
        }
    }

    private async void OnHotkey(int id)
    {
        if (_hotkeyProfiles.TryGetValue(id, out var profileId) && Automation.FindProfile(profileId) is { } profile)
        {
            ShortcutUsed?.Invoke(this, profile);
            await ApplyAsync(profile);
        }
    }
}
