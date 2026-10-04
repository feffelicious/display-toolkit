using System.Windows;
using Microsoft.Win32;

namespace DisplayToolkit.App.Services;

/// <summary>Applies the app theme (system, light or dark) to WPF's Fluent theme and tells windows when it changes.</summary>
internal sealed class ThemeService
{
    private readonly AppSettings _settings;
    private AppTheme? _applied;

    public ThemeService(AppSettings settings)
    {
        _settings = settings;

        // Only for a theme change: re-applying reloads every control template, which would, for example, drop a
        // slider that is being dragged while it saves its value.
        settings.Changed += (_, _) =>
        {
            if (settings.Current.Theme != _applied)
            {
                Apply();
            }
        };
        SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
        };
    }

    /// <summary>The effective theme changed: windows should update their DWM dark-mode attribute.</summary>
    public event EventHandler? Changed;

    /// <summary>Whether the app currently renders dark, resolving "system" from the Windows setting.</summary>
    public bool IsDark => _settings.Current.Theme switch
    {
        AppTheme.Dark => true,
        AppTheme.Light => false,
        _ => IsSystemDark(),
    };

    public void Apply()
    {
        _applied = _settings.Current.Theme;
        Application.Current.ThemeMode = _settings.Current.Theme switch
        {
            AppTheme.Dark => ThemeMode.Dark,
            AppTheme.Light => ThemeMode.Light,
            _ => ThemeMode.System,
        };
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static bool IsSystemDark()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is 0;
    }
}
