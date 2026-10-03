using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Threading;
using DisplayToolkit.App.Flyout;
using DisplayToolkit.App.Services;
using DisplayToolkit.App.Tray;
using DisplayToolkit.App.ViewModels;
using DisplayToolkit.App.ViewModels.Main;
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

        _host.Services.GetRequiredService<ThemeService>().Apply();

        var tray = _host.Services.GetRequiredService<TrayController>();
        tray.Start();
        _singleInstance.ListenForActivation(() => Dispatcher.BeginInvoke(() => tray.ToggleFlyout()));

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

        viewModel.EditCommand.Execute(null);
        await Task.Delay(400);
        DebugSnapshot.Save(flyout.Root, folder, "flyout-edit");
        viewModel.OpenAddPageCommand.Execute(null);
        await Task.Delay(400);
        DebugSnapshot.Save(flyout.Root, folder, "flyout-add");
        viewModel.CancelEditCommand.Execute(null);
        flyout.HideFlyout();

        // Main window: the whole window once, then every page at full height.
        var mainViewModel = _host.Services.GetRequiredService<MainWindowViewModel>();
        var window = new Views.MainWindow(mainViewModel, _host.Services.GetRequiredService<ThemeService>());
        window.ShowAndActivate();
        await Task.Delay(2500);
        DebugSnapshot.Save((FrameworkElement)window.Content, folder, "main-window");
        foreach (var item in mainViewModel.NavItems.Append(mainViewModel.SettingsItem))
        {
            mainViewModel.SelectedNavItem = item;
            await Task.Delay(2000);
            DebugSnapshot.Save(window.PageHost, folder, $"main-{item.Title.Replace(' ', '-').ToLowerInvariant()}");
            DebugSnapshot.Save((FrameworkElement)window.Content, folder, $"window-{item.Title.Replace(' ', '-').ToLowerInvariant()}");
        }
        if (mainViewModel.NavItems.Count > 0)
        {
            mainViewModel.SelectedNavItem = mainViewModel.NavItems[0];
            await Task.Delay(500);
            if (mainViewModel.Page is DisplayPageViewModel display)
            {
                display.OpenSixAxisCommand.Execute(null);
                await Task.Delay(1000);
                DebugSnapshot.Save(window.PageHost, folder, "main-six-axis");
                mainViewModel.SelectedNavItem = mainViewModel.NavItems[1];
                mainViewModel.SelectedNavItem = mainViewModel.NavItems[0];
                ((DisplayPageViewModel)mainViewModel.Page!).OpenMonitorInformationCommand.Execute(null);
                await Task.Delay(2500);
                DebugSnapshot.Save(window.PageHost, folder, "main-monitor-info");
            }
        }
        window.Close();
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
        services.AddSingleton<LayoutStore>();
        services.AddSingleton<MonitorService>();
        services.AddSingleton<MonitorContext>();
        services.AddSingleton<AppSettings>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindowLauncher>();
        services.AddSingleton<FlyoutViewModel>();
        services.AddSingleton<FlyoutWindow>();
        services.AddSingleton<TrayController>();

        return builder.Build();
    }
}
