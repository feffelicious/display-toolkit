namespace DisplayToolkit.Automation.Sun;

/// <summary>Turns a color temperature into red, green and blue gains relative to the panel's own white.</summary>
public static class ColorTemperature
{
    /// <summary>The white the panel shows with all gains at maximum.</summary>
    public const int NativeKelvin = 6500;

    /// <summary>
    /// Gains (0–1) that tint the panel's 6500 K white to <paramref name="kelvin"/>. Uses Tanner Helland's fit of the
    /// black-body color, which is close enough for a warm evening tint. Above 6500 K red and green drop instead.
    /// </summary>
    public static (double Red, double Green, double Blue) Gains(int kelvin)
    {
        var (red, green, blue) = BlackBody(Math.Clamp(kelvin, 1000, 40000));
        var (nativeRed, nativeGreen, nativeBlue) = BlackBody(NativeKelvin);
        return (Math.Min(1, red / nativeRed), Math.Min(1, green / nativeGreen), Math.Min(1, blue / nativeBlue));
    }

    private static (double Red, double Green, double Blue) BlackBody(int kelvin)
    {
        var t = kelvin / 100.0;
        var red = t <= 66 ? 255 : 329.698727446 * Math.Pow(t - 60, -0.1332047592);
        var green = t <= 66 ? (99.4708025861 * Math.Log(t)) - 161.1195681661 : 288.1221695283 * Math.Pow(t - 60, -0.0755148492);
        var blue = t >= 66 ? 255 : t <= 19 ? 0 : (138.5177312231 * Math.Log(t - 10)) - 305.0447927307;
        return (Math.Clamp(red, 0, 255), Math.Clamp(green, 0, 255), Math.Clamp(blue, 0, 255));
    }
}
