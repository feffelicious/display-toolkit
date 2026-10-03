using System.Windows;

namespace DisplayToolkit.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Placeholder until the tray and flyout land (M2): show a window so the app is visibly alive.
        var window = new Window
        {
            Title = "Display Toolkit",
            Width = 480,
            Height = 240,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = new System.Windows.Controls.TextBlock
            {
                Text = "Display Toolkit",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 28,
            },
        };
        window.Closed += (_, _) => Shutdown();
        window.Show();
    }
}
