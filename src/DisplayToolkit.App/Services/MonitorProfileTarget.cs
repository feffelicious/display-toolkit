using DisplayToolkit.App.ViewModels;
using DisplayToolkit.Automation.Profiles;
using DisplayToolkit.Core.Features;
using DisplayToolkit.Core.Monitors;

namespace DisplayToolkit.App.Services;

/// <summary>Applies profiles to a monitor through the same live state the UI shows.</summary>
internal sealed class MonitorProfileTarget(MonitorViewModel monitor) : IProfileTarget
{
    public bool IsHdrActive => monitor.IsHdrActive;

    public bool Supports(Feature feature) => monitor.Session.Supports(feature);

    public async Task<bool> SetWindowsHdrAsync(bool enabled)
    {
        if (monitor.WindowsHdr is null)
        {
            return false;
        }
        await monitor.SetWindowsHdrAsync(enabled);
        return monitor.IsHdrActive == enabled;
    }

    public Task<FeatureValue> WriteAsync(Feature feature, uint value) => monitor.Session.WriteAsync(feature, value);

    /// <summary>
    /// The current values of the given profile settings, skipping any that aren't known right now (not read yet, or
    /// locked while HDR is on).
    /// </summary>
    public static Dictionary<string, uint> Capture(MonitorViewModel monitor, IEnumerable<string> settingIds)
    {
        var values = new Dictionary<string, uint>();
        foreach (var setting in settingIds.Select(ProfileSettings.Find).OfType<ProfileSetting>())
        {
            if (setting.Feature is null)
            {
                if (monitor.WindowsHdr is not null)
                {
                    values[setting.Id] = monitor.IsHdrActive ? 1u : 0u;
                }
            }
            else if (monitor[setting.Feature] is { IsKnown: true, Status: FeatureStatus.Confirmed } state)
            {
                values[setting.Id] = state.Value;
            }
        }
        return values;
    }
}
