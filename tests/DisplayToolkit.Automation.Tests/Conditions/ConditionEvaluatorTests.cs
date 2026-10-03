using DisplayToolkit.Automation.Conditions;
using DisplayToolkit.Automation.Rules;

namespace DisplayToolkit.Automation.Tests.Conditions;

public sealed class ConditionEvaluatorTests
{
    private static readonly SystemSnapshot Snapshot = new(
        new HashSet<string>(["explorer", "VLC"], StringComparer.OrdinalIgnoreCase),
        IsFullscreenGameRunning: false,
        PowerSource.Battery,
        IsHdrOn: true);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Conditions_are_matched_against_the_snapshot(Trigger trigger, bool expected) =>
        Assert.Equal(expected, ConditionEvaluator.Holds(trigger, Snapshot));

    public static TheoryData<Trigger, bool> Cases() => new()
    {
        { new AppTrigger("vlc", "VLC media player"), true },
        { new AppTrigger("mpv", "mpv"), false },
        { new FullscreenGameTrigger(), false },
        { new PowerTrigger(PowerSource.Battery), true },
        { new PowerTrigger(PowerSource.PluggedIn), false },
        { new HdrTrigger(IsOn: true), true },
        { new HdrTrigger(IsOn: false), false },
        { new TimeTrigger(new TimeOnly(8, 0), Days.Every), false },
    };

    [Fact]
    public void Only_enabled_rules_are_active()
    {
        Rule[] rules =
        [
            new() { Id = Guid.NewGuid(), ProfileId = Guid.NewGuid(), Trigger = new HdrTrigger(IsOn: true) },
            new() { Id = Guid.NewGuid(), ProfileId = Guid.NewGuid(), Trigger = new HdrTrigger(IsOn: true), IsEnabled = false },
        ];

        Assert.Equal([rules[0].Id], ConditionEvaluator.ActiveRules(rules, Snapshot));
    }
}
