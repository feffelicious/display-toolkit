using DisplayToolkit.Automation.Sun;
using Microsoft.Extensions.Logging;
using Windows.Devices.Geolocation;

namespace DisplayToolkit.App.Services;

public enum LocationSource
{
    /// <summary>No location yet: sunrise and sunset rules can't run.</summary>
    None,
    Windows,
    Manual,
}

/// <summary>
/// Where the user is, for sunrise and sunset rules: from the Windows location service, or entered by hand. Only a
/// rounded position (about 1 km) is kept, which is all sun times need.
/// </summary>
internal sealed class LocationService(AppSettings settings, ILogger<LocationService> logger)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan MaximumAge = TimeSpan.FromHours(12);

    public event EventHandler? Changed;

    public GeoCoordinate? Current => ToCoordinate(settings.Current.UseWindowsLocation ? settings.Current.LastWindowsLocation : settings.Current.ManualLocation);

    public LocationSource Source => Current is null
        ? LocationSource.None
        : settings.Current.UseWindowsLocation ? LocationSource.Windows : LocationSource.Manual;

    /// <summary>True when Windows refused the location (turned off, or not allowed for desktop apps).</summary>
    public bool IsWindowsLocationUnavailable { get; private set; }

    /// <summary>Asks Windows for the current position, if the user chose Windows location. Never throws.</summary>
    public async Task RefreshAsync()
    {
        if (!settings.Current.UseWindowsLocation)
        {
            return;
        }

        try
        {
            if (await Geolocator.RequestAccessAsync() != GeolocationAccessStatus.Allowed)
            {
                SetUnavailable(true);
                return;
            }

            var locator = new Geolocator { DesiredAccuracy = PositionAccuracy.Default };
            var position = (await locator.GetGeopositionAsync(MaximumAge, Timeout)).Coordinate.Point.Position;
            var location = new SavedLocation(Math.Round(position.Latitude, 2), Math.Round(position.Longitude, 2));
            IsWindowsLocationUnavailable = false;
            if (location != settings.Current.LastWindowsLocation)
            {
                settings.Update(current => current with { LastWindowsLocation = location });
            }
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or TimeoutException or System.Runtime.InteropServices.COMException or TaskCanceledException)
        {
            logger.LogInformation(exception, "Windows location unavailable");
            SetUnavailable(true);
        }
    }

    public async Task UseWindowsLocationAsync()
    {
        settings.Update(current => current with { UseWindowsLocation = true });
        Changed?.Invoke(this, EventArgs.Empty);
        await RefreshAsync();
    }

    public void UseManualLocation(SavedLocation location)
    {
        settings.Update(current => current with { UseWindowsLocation = false, ManualLocation = location });
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void SetUnavailable(bool unavailable)
    {
        IsWindowsLocationUnavailable = unavailable;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static GeoCoordinate? ToCoordinate(SavedLocation? location) =>
        location is null ? null : new GeoCoordinate(location.Latitude, location.Longitude);
}
