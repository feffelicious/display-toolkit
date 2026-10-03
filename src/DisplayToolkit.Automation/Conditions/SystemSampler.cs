using System.Diagnostics;
using DisplayToolkit.Automation.Native;
using DisplayToolkit.Automation.Rules;

namespace DisplayToolkit.Automation.Conditions;

/// <summary>Takes a <see cref="SystemSnapshot"/> of this PC. Cheap enough to call every couple of seconds.</summary>
public static class SystemSampler
{
    /// <summary>
    /// Full-screen apps that aren't games. A browser playing a video full screen, or a slideshow, shouldn't switch to
    /// the gaming profile.
    /// </summary>
    private static readonly HashSet<string> NotGames = new(StringComparer.OrdinalIgnoreCase)
    {
        // Browsers
        "chrome", "msedge", "firefox", "brave", "opera", "opera_gx", "vivaldi", "arc", "librewolf", "waterfox", "zen",
        // Video and media
        "vlc", "mpv", "mpc-hc", "mpc-hc64", "mpc-be", "mpc-be64", "PotPlayerMini", "PotPlayerMini64", "wmplayer", "Video.UI",
        "Microsoft.Media.Player", "Plex", "Kodi", "Netflix", "Spotify",
        // Windows and productivity
        "explorer", "ApplicationFrameHost", "ShellExperienceHost", "StartMenuExperienceHost", "SearchHost", "LockApp",
        "POWERPNT", "WINWORD", "EXCEL", "ONENOTE", "Teams", "ms-teams", "Zoom", "obs64", "Code", "devenv",
    };

    /// <param name="isHdrOn">Whether Windows HDR is on for the controlled monitor (known to the caller).</param>
    public static SystemSnapshot Capture(bool isHdrOn) =>
        new(RunningApps(), IsFullscreenGameRunning(), CurrentPowerSource(), isHdrOn);

    public static HashSet<string> RunningApps()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                names.Add(process.ProcessName);
            }
        }
        return names;
    }

    /// <summary>
    /// An exclusive full-screen Direct3D app, or a foreground window covering its whole screen (borderless games)
    /// that isn't a known non-game.
    /// </summary>
    public static bool IsFullscreenGameRunning()
    {
        if (Win32.SHQueryUserNotificationState(out var state) == 0 && state == Win32.QunsRunningD3DFullScreen)
        {
            return true;
        }

        var window = Win32.GetForegroundWindow();
        if (window == 0 || window == Win32.GetShellWindow() || !Win32.GetWindowRect(window, out var bounds)
            || Win32.MonitorBoundsOf(window) is not { } screen)
        {
            return false;
        }

        var coversScreen = bounds.Left <= screen.Left && bounds.Top <= screen.Top
            && bounds.Right >= screen.Right && bounds.Bottom >= screen.Bottom;
        return coversScreen && ProcessNameOf(window) is { } name && !NotGames.Contains(name);
    }

    /// <summary>Null on devices without a battery, where the power source never changes.</summary>
    public static PowerSource? CurrentPowerSource()
    {
        if (!Win32.GetSystemPowerStatus(out var status) || (status.BatteryFlag & Win32.BatteryFlagNoBattery) != 0)
        {
            return null;
        }
        return status.AcLineStatus switch
        {
            0 => PowerSource.Battery,
            1 => PowerSource.PluggedIn,
            _ => null,
        };
    }

    public static bool HasBattery() => CurrentPowerSource() is not null;

    private static string? ProcessNameOf(nint window)
    {
        Win32.GetWindowThreadProcessId(window, out var processId);
        if (processId == Environment.ProcessId)
        {
            return null; // Our own windows (the flyout) are never games.
        }
        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (ArgumentException)
        {
            return null; // Exited in the meantime.
        }
    }
}
