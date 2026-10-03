namespace DisplayToolkit.Automation.Tests.Fakes;

/// <summary>A clock the test moves by hand, in a fixed time zone.</summary>
internal sealed class ManualTimeProvider(DateTimeOffset start, TimeZoneInfo zone) : TimeProvider
{
    /// <summary>Central European time (Stockholm), with daylight saving.</summary>
    public static TimeZoneInfo Stockholm { get; } = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

    private DateTimeOffset _now = start;

    public override TimeZoneInfo LocalTimeZone => zone;

    public override DateTimeOffset GetUtcNow() => _now.ToUniversalTime();

    public void Advance(TimeSpan duration) => _now += duration;

    public void SetLocal(DateTime local) => _now = new DateTimeOffset(local, zone.GetUtcOffset(local));

    /// <summary>A clock at <paramref name="local"/> Stockholm time.</summary>
    public static ManualTimeProvider AtStockholm(DateTime local) =>
        new(new DateTimeOffset(local, Stockholm.GetUtcOffset(local)), Stockholm);
}
