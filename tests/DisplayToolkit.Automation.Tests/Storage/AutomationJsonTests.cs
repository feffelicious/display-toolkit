using DisplayToolkit.Automation.Profiles;
using DisplayToolkit.Automation.Rules;
using DisplayToolkit.Automation.Storage;

namespace DisplayToolkit.Automation.Tests.Storage;

public sealed class AutomationJsonTests
{
    [Fact]
    public void Profiles_and_rules_survive_a_round_trip()
    {
        var night = new Profile
        {
            Id = Guid.NewGuid(),
            Name = "Night",
            Glyph = "",
            Shortcut = new Shortcut(ShortcutModifiers.Control | ShortcutModifiers.Alt, 0x32),
            Settings = new Dictionary<string, uint> { ["brightness"] = 20, [ProfileSettings.WindowsHdrId] = 0 },
        };
        Rule[] rules =
        [
            new() { Id = Guid.NewGuid(), ProfileId = night.Id, Trigger = new SunTrigger(SunEvent.Sunset, -30, Days.Every) },
            new() { Id = Guid.NewGuid(), ProfileId = night.Id, Trigger = new TimeTrigger(new TimeOnly(22, 15), Days.Weekdays), IsEnabled = false },
            new() { Id = Guid.NewGuid(), ProfileId = night.Id, Trigger = new AppTrigger("vlc", "VLC media player") },
            new() { Id = Guid.NewGuid(), ProfileId = night.Id, Trigger = new FullscreenGameTrigger() },
            new() { Id = Guid.NewGuid(), ProfileId = night.Id, Trigger = new PowerTrigger(PowerSource.Battery) },
            new() { Id = Guid.NewGuid(), ProfileId = night.Id, Trigger = new HdrTrigger(IsOn: true) },
        ];
        var data = new Dictionary<string, MonitorAutomation> { ["AUSAA6A"] = new() { Profiles = [night], Rules = rules } };

        var json = AutomationJson.Serialize(data);
        var restored = AutomationJson.Deserialize(json)["AUSAA6A"];

        Assert.Contains("\"type\": \"sun\"", json, StringComparison.Ordinal);
        Assert.Equal(rules, restored.Rules);
        var profile = Assert.Single(restored.Profiles);
        Assert.Equal(night with { Settings = profile.Settings }, profile);
        Assert.Equal(night.Settings, profile.Settings);
    }
}
