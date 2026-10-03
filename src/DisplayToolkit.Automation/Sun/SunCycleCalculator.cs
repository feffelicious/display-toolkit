namespace DisplayToolkit.Automation.Sun;

/// <summary>Works out where the sun cycle is at a moment, and what brightness and warmth that means.</summary>
public static class SunCycleCalculator
{
    /// <summary>
    /// How far into the night it is: 0 by day, 1 by night, and an eased fade in between during the transition around
    /// sunrise and sunset.
    /// </summary>
    public static double NightAmount(DateTimeOffset now, GeoCoordinate location, TimeZoneInfo zone, TimeSpan transition)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(zone);

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
        var sun = SunCalculator.Calculate(today, location, zone);
        if (sun.Sunrise is not { } sunrise || sun.Sunset is not { } sunset)
        {
            return sun.IsUpAllDay ? 0 : 1;
        }

        var half = transition / 2;
        if (now <= sunrise - half || now >= sunset + half)
        {
            return 1;
        }
        if (now < sunrise + half)
        {
            return 1 - Ease((now - (sunrise - half)) / transition);
        }
        if (now > sunset - half)
        {
            return Ease((now - (sunset - half)) / transition);
        }
        return 0;
    }

    /// <summary>The brightness and warmth for a point in the cycle (0 = day, 1 = night).</summary>
    public static SunCycleTarget Target(SunCycle cycle, double night)
    {
        ArgumentNullException.ThrowIfNull(cycle);
        night = Math.Clamp(night, 0, 1);

        uint? brightness = cycle.ControlsBrightness
            ? (uint)Math.Round(cycle.DayBrightness + ((cycle.NightBrightness - (double)cycle.DayBrightness) * night))
            : null;

        // Fade in mireds (1,000,000 / K): equal steps there look like equal steps in warmth.
        int? kelvin = null;
        if (cycle.ControlsWarmth)
        {
            var dayMireds = 1e6 / cycle.DayKelvin;
            var nightMireds = 1e6 / cycle.NightKelvin;
            kelvin = (int)Math.Round(1e6 / (dayMireds + ((nightMireds - dayMireds) * night)) / 50) * 50;
        }
        return new SunCycleTarget(night, brightness, kelvin);
    }

    /// <summary>Smooth start and end, so the fade doesn't begin or stop abruptly.</summary>
    private static double Ease(double x)
    {
        x = Math.Clamp(x, 0, 1);
        return x * x * (3 - (2 * x));
    }
}
