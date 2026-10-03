using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Threading;
using DisplayToolkit.App.Flyout;
using DisplayToolkit.App.Services;
using DisplayToolkit.App.Tray;
using DisplayToolkit.App.ViewModels;
using DisplayToolkit.Core.Monitors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DisplayToolkit.App;

[SuppressMessage("Design", "CA1001", Justification = "WPF owns the application lifetime; fields are disposed in OnExit.")]
public partial class App : Application
{
    private SingleInstance? _singleInstance;
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new SingleInstance();
        if (!_singleInstance.IsFirst)
        {
            _singleInstance.SignalFirstInstance();
            Shutdown();
            return;
        }

        _host = CreateHost(Dispatcher);
        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        DispatcherUnhandledException += (_, args) => logger.LogError(args.Exception, "Unhandled exception");
        logger.LogInformation("Display Toolkit starting");

        var tray = _host.Services.GetRequiredService<TrayController>();
        tray.Start();
        _singleInstance.ListenForActivation(() => Dispatcher.BeginInvoke(tray.ToggleFlyout));

        await _host.Services.GetRequiredService<MonitorService>().RescanAsync();

#if DEBUG
        if (DebugSnapshot.RequestedFolder(e.Args) is { } folder)
        {
            await CaptureSnapshotsAsync(folder);
        }
#endif
    }

#if DEBUG
    /// <summary>Renders the flyout's main page and each sub-page to PNGs, then exits.</summary>
    private async Task CaptureSnapshotsAsync(string folder)
    {
        var flyout = _host!.Services.GetRequiredService<FlyoutWindow>();
        var viewModel = _host.Services.GetRequiredService<FlyoutViewModel>();
        flyout.ShowFlyout();
        await Task.Delay(1500);
        DebugSnapshot.Save(flyout.Root, folder, "flyout-main");

        foreach (var tile in viewModel.Tiles.Where(tile => tile.Kind != ViewModels.Tiles.TileKind.Toggle))
        {
            tile.SecondaryCommand.Execute(null);
            await Task.Delay(400);
            DebugSnapshot.Save(flyout.Root, folder, $"flyout-{tile.Id}");
            viewModel.BackCommand.Execute(null);
        }
        Shutdown();
    }
#endif

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    private static IHost CreateHost(Dispatcher dispatcher)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new FileLoggerProvider());
        builder.Logging.AddDebug();

        var services = builder.Services;
        services.AddSingleton(dispatcher);
        services.AddSingleton<IMonitorEnumerator, Win32MonitorEnumerator>();
        services.AddSingleton<CapabilitiesCache>();
        services.AddSingleton<MonitorService>();
        services.AddSingleton<FlyoutViewModel>();
        services.AddSingleton<FlyoutWindow>();
        services.AddSingleton<TrayController>();

        return builder.Build();
    }
}
