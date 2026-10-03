using DisplayToolkit.Automation.Profiles;
using DisplayToolkit.Core.Features;
using DisplayToolkit.Core.Monitors;

namespace DisplayToolkit.Automation.Tests.Profiles;

public sealed class ProfileApplierTests
{
    private readonly RecordingTarget _target = new();

    [Fact]
    public async Task Settings_are_written_mode_first_then_color_temperature_then_the_rest()
    {
        var profile = Profile(
            (FeatureCatalog.RedGain.Id, 90),
            (FeatureCatalog.Brightness.Id, 40),
            (FeatureCatalog.ColorTemperature.Id, 11),
            (FeatureCatalog.PictureMode.Id, 4));

        var result = await ProfileApplier.ApplyAsync(profile, _target);

        Assert.True(result.Succeeded);
        Assert.Equal(["picture-mode=4", "color-temperature=11"], _target.Log.Take(2));
        Assert.Equal(["brightness=40", "red-gain=90"], _target.Log.Skip(2).Order());
    }

    [Fact]
    public async Task Windows_hdr_switches_first_and_only_when_needed()
    {
        var profile = Profile((FeatureCatalog.Contrast.Id, 70), (ProfileSettings.WindowsHdrId, 1));

        await ProfileApplier.ApplyAsync(profile, _target);
        await ProfileApplier.ApplyAsync(profile, _target);

        Assert.Equal("windows-hdr=True", _target.Log[0]);
        Assert.Single(_target.Log, entry => entry.StartsWith("windows-hdr", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Picture_settings_are_skipped_in_hdr_and_the_hdr_mode_in_sdr()
    {
        var profile = Profile(
            (FeatureCatalog.PictureMode.Id, 4),
            (FeatureCatalog.Brightness.Id, 40),
            (FeatureCatalog.HdrMode.Id, 0x0102),
            (FeatureCatalog.Contrast.Id, 70));

        await ProfileApplier.ApplyAsync(profile, _target);
        Assert.DoesNotContain("hdr-mode=258", _target.Log);

        _target.Log.Clear();
        _target.IsHdrActive = true;
        await ProfileApplier.ApplyAsync(profile, _target);
        Assert.Equal(["hdr-mode=258", "contrast=70"], _target.Log);
    }

    [Fact]
    public async Task Failures_are_reported_per_setting()
    {
        _target.Failing.Add(FeatureCatalog.ColorTemperature);

        var result = await ProfileApplier.ApplyAsync(Profile((FeatureCatalog.ColorTemperature.Id, 11), (FeatureCatalog.Brightness.Id, 40)), _target);

        Assert.Equal(["Color temperature"], result.Failed.Select(setting => setting.Name));
        Assert.Contains("brightness=40", _target.Log);
    }

    [Fact]
    public async Task Unsupported_and_unknown_settings_are_skipped()
    {
        _target.Unsupported.Add(FeatureCatalog.Elmb);

        var result = await ProfileApplier.ApplyAsync(Profile((FeatureCatalog.Elmb.Id, 1), ("no-such-setting", 3)), _target);

        Assert.True(result.Succeeded);
        Assert.Empty(_target.Log);
    }

    private static Profile Profile(params (string Id, uint Value)[] settings) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test",
        Settings = settings.ToDictionary(setting => setting.Id, setting => setting.Value),
    };

    private sealed class RecordingTarget : IProfileTarget
    {
        public List<string> Log { get; } = [];

        public HashSet<Feature> Failing { get; } = [];

        public HashSet<Feature> Unsupported { get; } = [];

        public bool IsHdrActive { get; set; }

        public bool Supports(Feature feature) => !Unsupported.Contains(feature);

        public Task<bool> SetWindowsHdrAsync(bool enabled)
        {
            Log.Add($"windows-hdr={enabled}");
            IsHdrActive = enabled;
            return Task.FromResult(true);
        }

        public Task<FeatureValue> WriteAsync(Feature feature, uint value)
        {
            Log.Add($"{feature.Id}={value}");
            var status = Failing.Contains(feature) ? FeatureStatus.Failed : FeatureStatus.Confirmed;
            return Task.FromResult(new FeatureValue(feature, value, 100, status));
        }
    }
}
