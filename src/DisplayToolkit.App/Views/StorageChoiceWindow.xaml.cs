using System.IO;
using System.Windows;
using System.Windows.Interop;
using DisplayToolkit.App.Native;
using DisplayToolkit.App.Services;

namespace DisplayToolkit.App.Views;

/// <summary>
/// Shown on the first start of the portable app (the zip), before anything is written: settings go next to the app or
/// into <c>%AppData%</c>. Either choice creates its folder, which answers the question for later starts. Closing the
/// window chooses nothing: <see cref="Window.ShowDialog"/> returns false and the app quits, to ask again next time.
/// </summary>
internal sealed partial class StorageChoiceWindow : Window
{
    public StorageChoiceWindow()
    {
        InitializeComponent();
        // Settings (and with them the app's own theme choice) don't exist yet: follow Windows.
        SourceInitialized += (_, _) => Dwm.SetDarkMode(new WindowInteropHelper(this).Handle, ThemeService.IsSystemDark());
        Loaded += (_, _) => PortableChoice.Focus();
    }

    private void OnPortableClick(object sender, RoutedEventArgs e)
    {
        try
        {
            AppPaths.CreatePortableFolder();
            DialogResult = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void OnThisPcClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
