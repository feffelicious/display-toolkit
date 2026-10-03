using DisplayToolkit.Automation.Rules;

namespace DisplayToolkit.Automation.Conditions;

/// <summary>What condition rules look at, sampled at one moment.</summary>
/// <param name="RunningApps">Executable names without ".exe". Compare case-insensitively.</param>
/// <param name="IsFullscreenGameRunning">A game (or a full-screen app that isn't a browser or video player) fills a screen.</param>
/// <param name="PowerSource">Null on devices without a battery.</param>
/// <param name="IsHdrOn">Windows HDR is on for the controlled monitor.</param>
public sealed record SystemSnapshot(
    IReadOnlySet<string> RunningApps,
    bool IsFullscreenGameRunning,
    PowerSource? PowerSource,
    bool IsHdrOn);
