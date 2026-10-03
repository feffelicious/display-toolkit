using DisplayToolkit.Automation.Sun;
using DisplayToolkit.Automation.Tests.Fakes;

namespace DisplayToolkit.Automation.Tests.Sun;

public sealed class SunCalculatorTests
{
    private static readonly GeoCoordinate Stockholm = new(59.3293, 18.0686);
    private static readonly GeoCoordinate Tromso = new(69.6492, 18.9553);
    private static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(3);

    [Fact]
    public void Stockholm_at_midsummer()
    {
        // Published times: sunrise 03:31, sunset 22:08 (CEST).
        var times = SunCalculator.Calculate(new DateOnly(2024, 6, 21), Stockholm, ManualTimeProvider.Stockholm);

        AssertNear(new DateTimeOffset(2024, 6, 21, 3, 31, 0, TimeSpan.FromHours(2)), times.Sunrise);
        AssertNear(new DateTimeOffset(2024, 6, 21, 22, 8, 0, TimeSpan.FromHours(2)), times.Sunset);
    }

    [Fact]
    public void Stockholm_in_winter_uses_standard_time()
    {
        // Published times: sunrise 08:43, sunset 14:48 (CET).
        var times = SunCalculator.Calculate(new DateOnly(2024, 12, 21), Stockholm, ManualTimeProvider.Stockholm);

        AssertNear(new DateTimeOffset(2024, 12, 21, 8, 43, 0, TimeSpan.FromHours(1)), times.Sunrise);
        AssertNear(new DateTimeOffset(2024, 12, 21, 14, 48, 0, TimeSpan.FromHours(1)), times.Sunset);
        Assert.Equal(TimeSpan.FromHours(1), times.Sunrise!.Value.Offset);
    }

    [Fact]
    public void Equator_at_the_equinox_has_a_day_of_about_twelve_hours()
    {
        var times = SunCalculator.Calculate(new DateOnly(2024, 3, 20), new GeoCoordinate(0, 0), TimeZoneInfo.Utc);

        AssertNear(new DateTimeOffset(2024, 3, 20, 6, 4, 0, TimeSpan.Zero), times.Sunrise);
        AssertNear(new DateTimeOffset(2024, 3, 20, 18, 10, 0, TimeSpan.Zero), times.Sunset);
    }

    [Fact]
    public void West_of_Greenwich_the_times_stay_on_the_local_date()
    {
        // Los Angeles: sunrise 05:42, sunset 20:08 (PDT).
        var losAngeles = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

        var times = SunCalculator.Calculate(new DateOnly(2024, 6, 21), new GeoCoordinate(34.0522, -118.2437), losAngeles);

        AssertNear(new DateTimeOffset(2024, 6, 21, 5, 42, 0, TimeSpan.FromHours(-7)), times.Sunrise);
        AssertNear(new DateTimeOffset(2024, 6, 21, 20, 8, 0, TimeSpan.FromHours(-7)), times.Sunset);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(12)]
    public void Polar_day_and_night_have_no_sunrise_or_sunset(int month)
    {
        var times = SunCalculator.Calculate(new DateOnly(2024, month, 21), Tromso, ManualTimeProvider.Stockholm);

        Assert.Null(times.Sunrise);
        Assert.Null(times.Sunset);
    }

    private static void AssertNear(DateTimeOffset expected, DateTimeOffset? actual)
    {
        Assert.NotNull(actual);
        var difference = (actual.Value - expected).Duration();
        Assert.True(difference <= Tolerance, $"Expected {expected:yyyy-MM-dd HH:mm zzz}, got {actual:yyyy-MM-dd HH:mm zzz}");
    }
}
