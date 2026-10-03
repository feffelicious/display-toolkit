using DisplayToolkit.Automation.Rules;

namespace DisplayToolkit.Automation.Conditions;

/// <summary>Decides which condition rules hold for a <see cref="SystemSnapshot"/>.</summary>
public static class ConditionEvaluator
{
    /// <summary>The ids of the enabled condition rules whose condition holds. Schedule rules are never included.</summary>
    public static IReadOnlyList<Guid> ActiveRules(IEnumerable<Rule> rules, SystemSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(snapshot);
        return [.. rules.Where(rule => rule.IsEnabled && Holds(rule.Trigger, snapshot)).Select(rule => rule.Id)];
    }

    public static bool Holds(Trigger trigger, SystemSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return trigger switch
        {
            AppTrigger app => snapshot.RunningApps.Contains(app.ProcessName),
            FullscreenGameTrigger => snapshot.IsFullscreenGameRunning,
            PowerTrigger power => snapshot.PowerSource == power.Source,
            HdrTrigger hdr => snapshot.IsHdrOn == hdr.IsOn,
            _ => false,
        };
    }
}
