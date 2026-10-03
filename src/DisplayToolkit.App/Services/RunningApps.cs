using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DisplayToolkit.App.Services;

/// <summary>An app a rule can watch for: its executable name (without ".exe"), a friendly name and its icon.</summary>
public sealed record AppChoice(string ProcessName, string DisplayName, ImageSource? Icon);

/// <summary>The apps the "An app is open" rule offers.</summary>
internal static class RunningApps
{
    /// <summary>Windows' own hosts and shell pieces, which have windows but aren't apps anyone picks.</summary>
    private static readonly HashSet<string> SystemHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "ApplicationFrameHost", "TextInputHost", "ShellExperienceHost", "StartMenuExperienceHost", "SearchHost", "LockApp",
        "explorer", "SystemSettingsBroker", "dwm", "ctfmon", "sihost", "RuntimeBroker",
    };

    /// <summary>Apps with a window right now, by name. Slow-ish (reads icons): call it off the UI thread.</summary>
    public static List<AppChoice> List()
    {
        var apps = new Dictionary<string, AppChoice>(StringComparer.OrdinalIgnoreCase);
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                if (process.Id == Environment.ProcessId || process.MainWindowHandle == 0 || string.IsNullOrEmpty(process.MainWindowTitle)
                    || SystemHosts.Contains(process.ProcessName) || apps.ContainsKey(process.ProcessName))
                {
                    continue;
                }
                var path = PathOf(process);
                apps[process.ProcessName] = path is null
                    ? new AppChoice(process.ProcessName, process.ProcessName, null)
                    : FromFile(path);
            }
        }
        return [.. apps.Values.OrderBy(app => app.DisplayName, StringComparer.CurrentCultureIgnoreCase)];
    }

    /// <summary>Describes an executable picked with Browse.</summary>
    public static AppChoice FromFile(string path)
    {
        var processName = Path.GetFileNameWithoutExtension(path);
        var description = FileVersionInfo.GetVersionInfo(path).FileDescription;
        return new AppChoice(processName, string.IsNullOrWhiteSpace(description) ? processName : description.Trim(), IconOf(path));
    }

    private static string? PathOf(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            return null; // Elevated or already exited.
        }
    }

    private static BitmapSource? IconOf(string path)
    {
        try
        {
            using var icon = Icon.ExtractAssociatedIcon(path);
            if (icon is null)
            {
                return null;
            }
            var image = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            image.Freeze(); // Created off the UI thread.
            return image;
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
