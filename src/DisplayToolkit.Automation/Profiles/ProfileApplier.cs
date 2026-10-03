using DisplayToolkit.Core.Features;
using DisplayToolkit.Core.Monitors;

namespace DisplayToolkit.Automation.Profiles;

/// <summary>The outcome of applying a profile. <see cref="Failed"/> lists the settings that didn't take effect.</summary>
public sealed record ProfileApplyResult(Profile Profile, IReadOnlyList<ProfileSetting> Failed)
{
    public bool Succeeded => Failed.Count == 0;
}

/// <summary>Writes a profile's settings to a monitor in <see cref="ApplyStage"/> order.</summary>
public static class ProfileApplier
{
    public static async Task<ProfileApplyResult> ApplyAsync(Profile profile, IProfileTarget target)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(target);

        var failed = new List<ProfileSetting>();
        var stages = ProfileSettings.All
            .Where(setting => profile.Settings.ContainsKey(setting.Id))
            .GroupBy(setting => setting.Stage)
            .OrderBy(stage => stage.Key);

        foreach (var stage in stages)
        {
            if (stage.Key == ApplyStage.WindowsHdr)
            {
                var enabled = profile.Settings[ProfileSettings.WindowsHdrId] != 0;
                if (enabled != target.IsHdrActive && !await target.SetWindowsHdrAsync(enabled))
                {
                    failed.Add(ProfileSettings.WindowsHdr);
                }
                continue;
            }

            // Settings within a stage don't depend on each other: queue them together, then wait for all of them.
            var writes = stage
                .Where(setting => IsApplicable(setting, target))
                .Select(async setting => (Setting: setting, Result: await target.WriteAsync(setting.Feature!, profile.Settings[setting.Id])));
            foreach (var (setting, result) in await Task.WhenAll(writes))
            {
                if (result.Status is not (FeatureStatus.Confirmed or FeatureStatus.AwaitingConfirmation))
                {
                    failed.Add(setting);
                }
            }
        }
        return new ProfileApplyResult(profile, failed);
    }

    /// <summary>
    /// Settings this monitor doesn't have, or that don't apply to the current signal (picture settings in HDR, the HDR
    /// mode in SDR), are skipped rather than reported as failures.
    /// </summary>
    private static bool IsApplicable(ProfileSetting setting, IProfileTarget target) =>
        setting.Feature is { IsReadOnly: false } feature
        && target.Supports(feature)
        && (target.IsHdrActive ? feature.AvailableInHdr : feature != FeatureCatalog.HdrMode);
}
