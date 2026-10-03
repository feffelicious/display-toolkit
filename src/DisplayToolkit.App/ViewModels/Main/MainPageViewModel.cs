using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DisplayToolkit.App.ViewModels.Main;

/// <summary>What pages can ask of the main window.</summary>
public interface IMainWindowHost
{
    /// <summary>Opens a sub-page (shown with a breadcrumb back to its parent).</summary>
    void Navigate(MainPageViewModel page);

    /// <summary>Shows a modal confirmation in the window. Returns true if the user chose the primary action.</summary>
    Task<bool> ConfirmAsync(ConfirmationViewModel confirmation);
}

/// <summary>
/// A page in the main window's content area. Pages are recreated on navigation while the monitor state they watch
/// lives on, so subscriptions go through <see cref="Observe"/> and are dropped by <see cref="Close"/>.
/// </summary>
public abstract class MainPageViewModel(string title) : ObservableObject
{
    private readonly List<Action> _unsubscribe = [];

    public string Title { get; } = title;

    /// <summary>For sub-pages: the page the breadcrumb leads back to.</summary>
    public MainPageViewModel? Parent { get; init; }

    /// <summary>Called when the page is shown; re-read what it displays.</summary>
    public virtual Task OnShownAsync() => Task.CompletedTask;

    /// <summary>Called when the window navigates away for good.</summary>
    public virtual void Close()
    {
        _unsubscribe.ForEach(unsubscribe => unsubscribe());
        _unsubscribe.Clear();
    }

    protected void Observe(INotifyPropertyChanged source, PropertyChangedEventHandler handler)
    {
        source.PropertyChanged += handler;
        _unsubscribe.Add(() => source.PropertyChanged -= handler);
    }
}

/// <summary>A modal "are you sure" with one primary action. Cancel is the safe default.</summary>
public sealed record ConfirmationViewModel(string Title, string Message, string PrimaryText, string? Detail = null);
