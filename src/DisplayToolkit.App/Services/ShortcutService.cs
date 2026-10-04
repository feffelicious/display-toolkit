using System.Globalization;
using System.Windows.Input;
using DisplayToolkit.Automation.Profiles;
using DisplayToolkit.Core.Features;
using Microsoft.Extensions.Logging;

namespace DisplayToolkit.App.Services;

/// <summary>Something a shortcut can do. <see cref="Id"/> is stored in the settings file: never rename one.</summary>
public sealed record ShortcutAction(string Id, string Name, string Glyph);

/// <summary>
/// The app's own shortcuts (profile shortcuts live with the profiles in <see cref="AutomationService"/>): open quick
/// settings, step brightness, cycle picture modes, switch HDR, Target mode and inputs. Each can be changed or removed
/// in Settings. Also steps brightness for the mouse wheel over the tray icon. Lives on the UI thread.
/// </summary>
internal sealed class ShortcutService : IDisposable
{
    public const string QuickSettings = "quick-settings";
    private const string BrightnessUp = "brightness-up";
    private const string BrightnessDown = "brightness-down";
    private const string NextPictureMode = "next-picture-mode";
    private const string ToggleHdr = "hdr";
    private const string ToggleTargetMode = "target-mode";
    private const string InputPrefix = "input-";

    /// <summary>Hotkey ids 10–99 belong to this service (the profiles use 100 and up).</summary>
    private const int FirstHotkeyId = 10;

    /// <summary>One shortcut press or wheel notch moves brightness this far, landing on multiples of it.</summary>
    private const int BrightnessStep = 5;

    private const string BrightnessGlyph = "";

    private static readonly Shortcut DefaultQuickSettings = new(ShortcutModifiers.Control | ShortcutModifiers.Alt, KeyInterop.VirtualKeyFromKey(Key.D));

    private static readonly ShortcutAction[] FixedActions =
    [
        new(QuickSettings, "Open quick settings", ""),
        new(BrightnessUp, "Brightness up", BrightnessGlyph),
        new(BrightnessDown, "Brightness down", BrightnessGlyph),
        new(NextPictureMode, "Next picture mode", ""),
        new(ToggleHdr, "Turn HDR on or off", "HDR"),
        new(ToggleTargetMode, "Target mode", ""),
    ];

    private readonly GlobalHotkeys _hotkeys;
    private readonly AppSettings _settings;
    private readonly MonitorContext _context;
    private readonly TargetMode _targetMode;
    private readonly ILogger<ShortcutService> _logger;
    private readonly Dictionary<int, string> _registered = [];
    private readonly HashSet<string> _taken = [];
    private Dictionary<string, Shortcut> _active = [];

    public ShortcutService(GlobalHotkeys hotkeys, AppSettings settings, MonitorContext context, TargetMode targetMode, ILogger<ShortcutService> logger)
    {
        _hotkeys = hotkeys;
        _settings = settings;
        _context = context;
        _targetMode = targetMode;
        _logger = logger;
        hotkeys.Pressed += (_, id) => OnPressed(id);
    }

    /// <summary>The quick settings shortcut was pressed.</summary>
    public event EventHandler? QuickSettingsRequested;

    /// <summary>A shortcut changed something, for the on-screen overlay.</summary>
    public event EventHandler<ShortcutFeedback>? Used;

    /// <summary>Shortcuts were changed or registered again.</summary>
    public event EventHandler? Changed;

    /// <summary>Everything a shortcut can do with the current monitor: the fixed actions, then one per input.</summary>
    public IReadOnlyList<ShortcutAction> Actions => [.. FixedActions, .. InputActions()];

    public void Start()
    {
        _settings.Changed += (_, _) => Register();
        _context.Changed += (_, _) => Register();
        Register();
    }

    /// <summary>The action's shortcut: the user's, or the default if they never changed it.</summary>
    public Shortcut? Get(string actionId) =>
        _settings.Current.Shortcuts.TryGetValue(actionId, out var shortcut) ? shortcut
        : actionId == QuickSettings ? DefaultQuickSettings
        : null;

    /// <summary>Sets or (with null) removes an action's shortcut.</summary>
    public void Set(string actionId, Shortcut? shortcut) =>
        _settings.Update(current => current with { Shortcuts = new(current.Shortcuts) { [actionId] = shortcut } });

    /// <summary>Windows or another app already uses the action's shortcut, so it doesn't work.</summary>
    public bool IsTaken(string actionId) => _taken.Contains(actionId);

    /// <summary>
    /// Moves brightness by <paramref name="steps"/> steps (negative to dim), landing on multiples of the step, and
    /// confirms it on screen. Used by the shortcuts and the mouse wheel over the tray icon.
    /// </summary>
    public void StepBrightness(int steps)
    {
        if (steps == 0 || _context.Current?[FeatureCatalog.Brightness] is not { } brightness)
        {
            return;
        }
        if (!brightness.IsAvailable)
        {
            Used?.Invoke(this, new ShortcutFeedback(BrightnessGlyph, brightness.IsLocked ? "Locked in HDR" : "Brightness"));
            return;
        }

        var value = (int)brightness.Value;
        var stepped = steps > 0
            ? ((value / BrightnessStep) + steps) * BrightnessStep
            : (((value + BrightnessStep - 1) / BrightnessStep) + steps) * BrightnessStep;
        var target = (uint)Math.Clamp(stepped, 0, (int)brightness.Maximum);
        brightness.Write(target);
        Used?.Invoke(this, new ShortcutFeedback(
            BrightnessGlyph, target.ToString(CultureInfo.CurrentCulture), Level: brightness.Maximum > 0 ? target / (double)brightness.Maximum : null));
    }

    public void Dispose()
    {
        foreach (var id in _registered.Keys)
        {
            _hotkeys.Unregister(id);
        }
    }

    private IEnumerable<ShortcutAction> InputActions() =>
        _context.Current is { } monitor && monitor[FeatureCatalog.InputSource] is not null
            ? monitor.OptionsOf(FeatureCatalog.InputSource).Select(option =>
                new ShortcutAction(InputPrefix + option.Value.ToString(CultureInfo.InvariantCulture), $"Switch to {option.Name}", ""))
            : [];

    /// <summary>Registers what the settings ask for, if that changed since last time.</summary>
    private void Register()
    {
        var wanted = new Dictionary<string, Shortcut>();
        foreach (var action in Actions)
        {
            if (Get(action.Id) is { } shortcut)
            {
                wanted[action.Id] = shortcut;
            }
        }
        if (wanted.Count == _active.Count && wanted.All(pair => _active.TryGetValue(pair.Key, out var active) && active == pair.Value))
        {
            return;
        }

        foreach (var id in _registered.Keys)
        {
            _hotkeys.Unregister(id);
        }
        _registered.Clear();
        _taken.Clear();
        _active = wanted;

        var nextId = FirstHotkeyId;
        foreach (var (actionId, shortcut) in wanted)
        {
            var id = nextId++;
            var repeats = actionId is BrightnessUp or BrightnessDown;
            if (_hotkeys.Register(id, (ModifierKeys)shortcut.Modifiers, KeyInterop.KeyFromVirtualKey(shortcut.VirtualKey), repeats))
            {
                _registered[id] = actionId;
            }
            else
            {
                _taken.Add(actionId);
                _logger.LogWarning("The shortcut for {Action} is taken", actionId);
            }
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnPressed(int id)
    {
        if (!_registered.TryGetValue(id, out var actionId))
        {
            return;
        }
        switch (actionId)
        {
            case QuickSettings:
                QuickSettingsRequested?.Invoke(this, EventArgs.Empty);
                break;
            case BrightnessUp:
                StepBrightness(1);
                break;
            case BrightnessDown:
                StepBrightness(-1);
                break;
            case NextPictureMode:
                CyclePictureMode();
                break;
            case ToggleHdr:
                SwitchHdr();
                break;
            case ToggleTargetMode:
                _targetMode.Toggle();
                Used?.Invoke(this, new ShortcutFeedback("", _targetMode.IsOn ? "Target mode on" : "Target mode off"));
                break;
            default:
                if (actionId.StartsWith(InputPrefix, StringComparison.Ordinal)
                    && uint.TryParse(actionId.AsSpan(InputPrefix.Length), CultureInfo.InvariantCulture, out var input))
                {
                    SwitchInput(input);
                }
                break;
        }
    }

    /// <summary>The next picture mode, or in HDR the next HDR preset for the signal being received.</summary>
    private void CyclePictureMode()
    {
        if (_context.Current is not { } monitor)
        {
            return;
        }

        var state = monitor.IsHdrActive ? monitor[FeatureCatalog.HdrMode] : monitor[FeatureCatalog.PictureMode];
        if (state is not { IsKnown: true })
        {
            return;
        }
        IReadOnlyList<FeatureOption> options = monitor.IsHdrActive
            ? [.. state.Options.Where(option => HdrModeFeature.GroupOf(option.Value) == HdrModeFeature.GroupOf(state.Value) && HdrModeFeature.GroupOf(option.Value) != 0)]
            : state.Options;
        if (options.Count == 0 || (!monitor.IsHdrActive && state.IsLocked))
        {
            return;
        }

        var index = options.Select(option => option.Value).ToList().IndexOf(state.Value);
        var next = options[(index + 1) % options.Count];
        state.Write(next.Value);
        Used?.Invoke(this, new ShortcutFeedback("", next.Name));
    }

    private void SwitchHdr()
    {
        if (_context.Current is not { WindowsHdr.IsSupported: true } monitor)
        {
            return;
        }
        var on = !monitor.IsHdrActive;
        Used?.Invoke(this, new ShortcutFeedback("HDR", on ? "HDR on" : "HDR off"));
        _ = monitor.SetWindowsHdrAsync(on);
    }

    private void SwitchInput(uint value)
    {
        if (_context.Current is not { } monitor || monitor[FeatureCatalog.InputSource] is null)
        {
            return;
        }
        var option = monitor.OptionsOf(FeatureCatalog.InputSource).FirstOrDefault(option => option.Value == value);
        if (option is null)
        {
            return;
        }
        Used?.Invoke(this, new ShortcutFeedback("", option.Name));

        // Straight to the monitor: the input register isn't reliable while input auto-detection is on, so the value
        // it reports may already match and a state write would be skipped.
        _ = monitor.Session.WriteAsync(FeatureCatalog.InputSource, value);
    }
}
