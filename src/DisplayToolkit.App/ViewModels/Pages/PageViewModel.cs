using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DisplayToolkit.App.ViewModels.Pages;

/// <summary>
/// A flyout sub-page, opened from a tile's chevron or a picker tile. Pages are short-lived while the states they watch
/// live as long as the monitor, so subscriptions go through <see cref="Observe"/> and are dropped by <see cref="Close"/>.
/// </summary>
public abstract class PageViewModel(string title) : ObservableObject
{
    private readonly List<Action> _unsubscribe = [];

    public string Title { get; } = title;

    /// <summary>Called by the flyout when the page is left.</summary>
    public void Close()
    {
        _unsubscribe.ForEach(unsubscribe => unsubscribe());
        _unsubscribe.Clear();
    }

    protected void Observe(INotifyPropertyChanged source, Action onChanged)
    {
        PropertyChangedEventHandler handler = (_, _) => onChanged();
        source.PropertyChanged += handler;
        _unsubscribe.Add(() => source.PropertyChanged -= handler);
    }
}
