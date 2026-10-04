namespace DisplayToolkit.Automation.Sun;

/// <summary>
/// Sunrise and sunset in local time. Both are null when the sun doesn't rise or set that day; then
/// <see cref="IsUpAllDay"/> tells polar day from polar night.
/// </summary>
public readonly record struct SunTimes(DateTimeOffset? Sunrise, DateTimeOffset? Sunset, bool IsUpAllDay = false);

/// <summary>
/// Sunrise and sunset from the sunrise equation with the standard corrections for refraction and the sun's disc
/// (the sun's upper edge at −0.833°). Accurate to about a minute away from the poles, which is plenty for switching a
/// monitor profile.
/// </summary>
public static class SunCalculator
{
    private const double J2000 = 2451545.0;
    private const double UnixEpochJulian = 2440587.5;
    private const double JulianDayOfDayNumberZero = 1721425.5;
    private const double ObliquityDegrees = 23.4397;
    private const double HorizonDegrees = -0.833;

    public static SunTimes Calculate(DateOnly date, GeoCoordinate location, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(zone);

        // Days since J2000 (noon, 1 January 2000), then the mean solar noon at this longitude.
        var julianDate = date.DayNumber + JulianDayOfDayNumberZero;
        var day = Math.Ceiling(julianDate - J2000 + 0.0008);
        var meanNoon = day - (location.Longitude / 360);

        var meanAnomaly = Normalize(357.5291 + (0.98560028 * meanNoon));
        var m = Radians(meanAnomaly);
        var center = (1.9148 * Math.Sin(m)) + (0.0200 * Math.Sin(2 * m)) + (0.0003 * Math.Sin(3 * m));
        var eclipticLongitude = Radians(Normalize(meanAnomaly + center + 180 + 102.9372));
        var transit = J2000 + meanNoon + (0.0053 * Math.Sin(m)) - (0.0069 * Math.Sin(2 * eclipticLongitude));

        var declination = Math.Asin(Math.Sin(eclipticLongitude) * Math.Sin(Radians(ObliquityDegrees)));
        var latitude = Radians(location.Latitude);
        var cosHourAngle = (Math.Sin(Radians(HorizonDegrees)) - (Math.Sin(latitude) * Math.Sin(declination)))
            / (Math.Cos(latitude) * Math.Cos(declination));

        // Outside [-1, 1] the sun stays above (polar day) or below (polar night) the horizon all day.
        if (cosHourAngle is < -1 or > 1)
        {
            return new SunTimes(null, null, IsUpAllDay: cosHourAngle < -1);
        }

        var hourAngle = Degrees(Math.Acos(cosHourAngle)) / 360;
        return new SunTimes(ToLocal(transit - hourAngle, zone), ToLocal(transit + hourAngle, zone));
    }

    /// <summary>
    /// The sun's height above the horizon in degrees (negative below it) at <paramref name="moment"/>, and the height it
    /// reaches at solar noon that day. Low-precision solar coordinates: within a few hundredths of a degree.
    /// </summary>
    public static (double Elevation, double NoonElevation) Elevation(DateTimeOffset moment, GeoCoordinate location)
    {
        ArgumentNullException.ThrowIfNull(location);

        var days = moment.UtcDateTime.ToOADate() + 2415018.5 - J2000; // Days since J2000, with the time of day
        var meanAnomaly = Radians(Normalize(357.529 + (0.98560028 * days)));
        var meanLongitude = Normalize(280.459 + (0.98564736 * days));
        var eclipticLongitude = Radians(meanLongitude + (1.915 * Math.Sin(meanAnomaly)) + (0.020 * Math.Sin(2 * meanAnomaly)));
        var obliquity = Radians(23.439 - (0.00000036 * days));

        var rightAscension = Degrees(Math.Atan2(Math.Cos(obliquity) * Math.Sin(eclipticLongitude), Math.Cos(eclipticLongitude)));
        var declination = Math.Asin(Math.Sin(obliquity) * Math.Sin(eclipticLongitude));
        var siderealDegrees = Normalize((280.46061837 + (360.98564736629 * days)) + location.Longitude);
        var hourAngle = Radians(siderealDegrees - rightAscension);

        var latitude = Radians(location.Latitude);
        var elevation = Math.Asin((Math.Sin(latitude) * Math.Sin(declination)) + (Math.Cos(latitude) * Math.Cos(declination) * Math.Cos(hourAngle)));
        var noon = 90 - Math.Abs(location.Latitude - Degrees(declination));
        return (Degrees(elevation), Math.Min(90, noon));
    }

    private static DateTimeOffset ToLocal(double julian, TimeZoneInfo zone)
    {
        var utc = DateTimeOffset.UnixEpoch.AddDays(julian - UnixEpochJulian);
        return TimeZoneInfo.ConvertTime(new DateTimeOffset(utc.Ticks - (utc.Ticks % TimeSpan.TicksPerSecond), TimeSpan.Zero), zone);
    }

    private static double Normalize(double degrees) => ((degrees % 360) + 360) % 360;

    private static double Radians(double degrees) => degrees * Math.PI / 180;

    private static double Degrees(double radians) => radians * 180 / Math.PI;
}
