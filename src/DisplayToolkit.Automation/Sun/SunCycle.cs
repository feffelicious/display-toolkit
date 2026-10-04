namespace DisplayToolkit.Automation.Sun;

/// <summary>
/// "Follow the sun": brightness and warmth hold day values while the sun is up and night values while it's down, and
/// fade between them over <see cref="TransitionMinutes"/> centered on sunrise and sunset.
/// </summary>
/// <remarks>
/// Properties with defaults are settable rather than init-only: the JSON source generator assigns every init-only
/// property when reading, so a file written by an older version would replace the defaults with empty values.
/// </remarks>
public sealed record SunCycle
{
    public static SunCycle Default { get; } = new();

    public bool IsEnabled { get; init; }

    public SunCycleMode Mode { get; set; } = SunCycleMode.SunriseAndSunset;

    public bool ControlsBrightness { get; set; } = true;

    public bool ControlsWarmth { get; set; } = true;

    public uint DayBrightness { get; set; } = 80;

    public uint NightBrightness { get; set; } = 35;

    /// <summary>Color temperature in kelvin. 6500 K is the panel's own white.</summary>
    public int DayKelvin { get; set; } = 6500;

    public int NightKelvin { get; set; } = 3400;

    public int TransitionMinutes { get; set; } = 60;
}

/// <summary>What the sun cycle wants right now; null for what it doesn't control.</summary>
/// <param name="Night">0 by day, 1 by night.</param>
public readonly record struct SunCycleTarget(double Night, uint? Brightness, int? Kelvin);

public enum SunCycleMode
{
    /// <summary>Day values all day, night values all night, and a fade of <see cref="SunCycle.TransitionMinutes"/> between.</summary>
    SunriseAndSunset,

    /// <summary>Follows the sun's height all day: day values at solar noon, night values from civil dusk to dawn.</summary>
    SunHeight,
}
