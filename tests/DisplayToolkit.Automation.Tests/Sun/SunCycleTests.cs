using DisplayToolkit.Automation.Sun;
using DisplayToolkit.Automation.Tests.Fakes;

namespace DisplayToolkit.Automation.Tests.Sun;

public sealed class SunCycleTests
{
    private static readonly GeoCoordinate Stockholm = new(59.3293, 18.0686);
    private static readonly TimeSpan Hour = TimeSpan.FromHours(1);
    private static readonly TimeZoneInfo Zone = ManualTimeProvider.Stockholm;

    // 7 October 2026 in Stockholm: sunrise about 07:24, sunset about 18:05 (CEST).
    private static readonly SunTimes Today = SunCalculator.Calculate(new DateOnly(2026, 10, 7), Stockholm, Zone);

    [Fact]
    public void Day_and_night_hold_their_values()
    {
        Assert.Equal(0, Night(At(12, 0)));
        Assert.Equal(1, Night(At(23, 0)));
        Assert.Equal(1, Night(At(3, 0)));
    }

    [Fact]
    public void Sunset_is_the_middle_of_the_fade()
    {
        var sunset = Today.Sunset!.Value;

        Assert.Equal(0, Night(sunset - TimeSpan.FromMinutes(31)));
        Assert.Equal(0.5, Night(sunset), precision: 3);
        Assert.Equal(1, Night(sunset + TimeSpan.FromMinutes(31)));
        Assert.InRange(Night(sunset - TimeSpan.FromMinutes(15)), 0.05, 0.3);
    }

    [Fact]
    public void Sunrise_fades_back_to_day()
    {
        var sunrise = Today.Sunrise!.Value;

        Assert.Equal(0.5, Night(sunrise), precision: 3);
        Assert.True(Night(sunrise + TimeSpan.FromMinutes(15)) < 0.5);
    }

    [Fact]
    public void Polar_summer_is_all_day_and_polar_winter_all_night()
    {
        var tromso = new GeoCoordinate(69.6492, 18.9553);

        Assert.Equal(0, SunCycleCalculator.NightAmount(new DateTimeOffset(2026, 6, 21, 23, 0, 0, TimeSpan.FromHours(2)), tromso, Zone, Hour));
        Assert.Equal(1, SunCycleCalculator.NightAmount(new DateTimeOffset(2026, 12, 21, 12, 0, 0, TimeSpan.FromHours(1)), tromso, Zone, Hour));
    }

    [Fact]
    public void Targets_blend_between_day_and_night()
    {
        var cycle = new SunCycle { IsEnabled = true, DayBrightness = 80, NightBrightness = 30, DayKelvin = 6500, NightKelvin = 3400 };

        Assert.Equal(new SunCycleTarget(0, 80, 6500), SunCycleCalculator.Target(cycle, 0));
        Assert.Equal(new SunCycleTarget(1, 30, 3400), SunCycleCalculator.Target(cycle, 1));

        var middle = SunCycleCalculator.Target(cycle, 0.5);
        Assert.Equal(55u, middle.Brightness);
        // Halfway in mireds is warmer than halfway in kelvin (4950 K).
        Assert.InRange(middle.Kelvin!.Value, 4400, 4500);
    }

    [Fact]
    public void Settings_the_cycle_doesnt_control_are_left_alone()
    {
        var target = SunCycleCalculator.Target(new SunCycle { ControlsBrightness = false, ControlsWarmth = false }, 0.5);

        Assert.Null(target.Brightness);
        Assert.Null(target.Kelvin);
    }

    [Fact]
    public void The_native_white_keeps_all_gains_at_maximum_and_warm_white_cuts_blue()
    {
        Assert.Equal((1.0, 1.0, 1.0), ColorTemperature.Gains(6500));

        var (red, green, blue) = ColorTemperature.Gains(3400);
        Assert.Equal(1.0, red);
        Assert.InRange(green, 0.7, 0.8);
        Assert.InRange(blue, 0.5, 0.6);
    }

    [Fact]
    public void The_sun_is_highest_at_solar_noon()
    {
        // Midsummer in Stockholm: solar noon about 13:13 CEST, at 90 - 59.33 + 23.44 = 54.1 degrees.
        var (elevation, noon) = SunCalculator.Elevation(new DateTimeOffset(2024, 6, 21, 13, 13, 0, TimeSpan.FromHours(2)), Stockholm);

        Assert.InRange(elevation, 53.6, 54.6);
        Assert.InRange(noon, 53.6, 54.6);
    }

    [Fact]
    public void The_sun_is_on_the_horizon_at_sunset()
    {
        // Published sunset includes refraction and the sun's disc, which put the center about 0.83 degrees below.
        var (elevation, _) = SunCalculator.Elevation(Today.Sunset!.Value, Stockholm);

        Assert.InRange(elevation, -1.3, -0.4);
    }

    [Fact]
    public void Sun_height_mode_follows_the_whole_day()
    {
        var cycle = SunCycle.Default with { Mode = SunCycleMode.SunHeight };
        double At(int hour) => SunCycleCalculator.NightAmount(cycle, new DateTimeOffset(2026, 10, 7, hour, 0, 0, TimeSpan.FromHours(2)), Stockholm, Zone);

        Assert.Equal(1, At(1));
        Assert.InRange(At(13), 0, 0.02); // Solar noon is about 12:46 CEST.
        Assert.InRange(At(16), 0.3, 0.7);
        Assert.True(At(10) > At(12));
        Assert.Equal(1, At(21));
    }

    [Fact]
    public void Sun_height_mode_stays_night_in_polar_winter()
    {
        var cycle = SunCycle.Default with { Mode = SunCycleMode.SunHeight };
        var svalbard = new GeoCoordinate(78.22, 15.65);

        Assert.Equal(1, SunCycleCalculator.NightAmount(cycle, new DateTimeOffset(2026, 12, 21, 12, 0, 0, TimeSpan.FromHours(1)), svalbard, Zone));
    }

    private static double Night(DateTimeOffset moment) => SunCycleCalculator.NightAmount(moment, Stockholm, Zone, Hour);

    private static DateTimeOffset At(int hour, int minute) => new(2026, 10, 7, hour, minute, 0, TimeSpan.FromHours(2));
}
