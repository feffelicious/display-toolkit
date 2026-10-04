using System.Globalization;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

namespace DisplayToolkit.App.Services;

/// <summary>A newer release on GitHub.</summary>
public sealed record UpdateInfo(Version Version, string Name, Uri Page);

/// <summary>
/// Looks for a newer release on GitHub: shortly after start, then at most once a day, unless turned off in Settings.
/// It only reads the latest release's version and page (pre-releases are skipped); downloading is up to the user.
/// Lives on the UI thread.
/// </summary>
internal sealed class UpdateService : IDisposable
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/feffelicious/display-toolkit/releases/latest";

    /// <summary>For development, <c>DISPLAYTOOLKIT_UPDATE_URL</c> points the check at another release (any public repository).</summary>
    private static readonly string ReleaseUrl = Environment.GetEnvironmentVariable("DISPLAYTOOLKIT_UPDATE_URL") is { Length: > 0 } custom
        ? custom
        : LatestReleaseUrl;

    private static readonly TimeSpan FirstCheckDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromDays(1);

    /// <summary>How often to wake up and see whether a day has passed (survives sleep better than a 24 h timer).</summary>
    private static readonly TimeSpan WakeInterval = TimeSpan.FromHours(1);

    private readonly AppSettings _settings;
    private readonly ILogger<UpdateService> _logger;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };
    private readonly DispatcherTimer _timer = new();
    private bool _isChecking;

    public UpdateService(AppSettings settings, ILogger<UpdateService> logger)
    {
        _settings = settings;
        _logger = logger;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd($"DisplayToolkit/{CurrentVersion.ToString(3)}");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        _timer.Tick += async (_, _) =>
        {
            _timer.Interval = WakeInterval;
            await CheckIfDueAsync();
        };
    }

    /// <summary>Something changed: a check finished or an update was dismissed.</summary>
    public event EventHandler? Changed;

    public static Version CurrentVersion { get; } = Normalize(Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0));

    /// <summary>The newer release, if the last check found one.</summary>
    public UpdateInfo? Available { get; private set; }

    /// <summary>Available, and not dismissed in quick settings.</summary>
    public bool ShouldRemind => Available is { } update && _settings.Current.DismissedUpdateVersion != update.Version.ToString(3);

    public bool IsChecking => _isChecking;

    /// <summary>"You're up to date", "Couldn't check …", or empty before the first check.</summary>
    public string Status { get; private set; } = string.Empty;

    public void Start()
    {
        _timer.Interval = FirstCheckDelay;
        _timer.Start();
    }

    /// <summary>Checks now, whatever the setting and the time of the last check.</summary>
    public Task CheckNowAsync() => CheckAsync();

    /// <summary>Opens the release page in the browser, where the user downloads the installer or the zip.</summary>
    public void OpenReleasePage()
    {
        if (Available is { } update)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(update.Page.AbsoluteUri) { UseShellExecute = true });
        }
    }

    /// <summary>Stops reminding about this version in quick settings (Settings still shows it).</summary>
    public void Dismiss()
    {
        if (Available is { } update)
        {
            _settings.Update(current => current with { DismissedUpdateVersion = update.Version.ToString(3) });
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _http.Dispose();
    }

    private async Task CheckIfDueAsync()
    {
        var last = _settings.Current.LastUpdateCheck;
        if (_settings.Current.CheckForUpdates && (last is null || DateTimeOffset.Now - last >= CheckInterval))
        {
            await CheckAsync();
        }
    }

    private async Task CheckAsync()
    {
        if (_isChecking)
        {
            return;
        }

        _isChecking = true;
        Changed?.Invoke(this, EventArgs.Empty);
        try
        {
            using var response = await _http.GetAsync(new Uri(ReleaseUrl));
            if (!response.IsSuccessStatusCode)
            {
                // 404 also means "no public release yet" (or a private repository).
                Status = "Couldn't check for updates: GitHub has no public release to compare with.";
                _logger.LogInformation("Update check: GitHub answered {Status}", (int)response.StatusCode);
                return;
            }

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = json.RootElement;
            var tag = root.GetProperty("tag_name").GetString() ?? string.Empty;
            var page = new Uri(root.GetProperty("html_url").GetString() ?? throw new JsonException("No release page."));
            var name = root.TryGetProperty("name", out var nameProperty) && nameProperty.GetString() is { Length: > 0 } title ? title : tag;

            _settings.Update(current => current with { LastUpdateCheck = DateTimeOffset.Now });
            if (Version.TryParse(tag.TrimStart('v', 'V'), out var version) && Normalize(version) > CurrentVersion)
            {
                Available = new UpdateInfo(Normalize(version), name, page);
                Status = string.Create(CultureInfo.CurrentCulture, $"Version {Available.Version.ToString(3)} is available.");
                _logger.LogInformation("Update available: {Version}", Available.Version);
            }
            else
            {
                Available = null;
                Status = string.Create(CultureInfo.CurrentCulture, $"You're up to date. Checked {DateTime.Now:t}.");
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException or UriFormatException)
        {
            Status = "Couldn't check for updates. Are you offline?";
            _logger.LogInformation(exception, "Update check failed");
        }
        finally
        {
            _isChecking = false;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Three parts: "1.0" from a tag and "1.0.0.0" from the assembly compare equal.</summary>
    private static Version Normalize(Version version) => new(version.Major, Math.Max(version.Minor, 0), Math.Max(version.Build, 0));
}
