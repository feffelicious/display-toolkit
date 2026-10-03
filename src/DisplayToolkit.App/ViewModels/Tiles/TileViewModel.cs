using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DisplayToolkit.Core.Features;

namespace DisplayToolkit.App.ViewModels.Tiles;

public enum TileKind
{
    /// <summary>Click toggles.</summary>
    Toggle,

    /// <summary>Main area toggles; the chevron opens a page with levels or styles.</summary>
    Split,

    /// <summary>Shows the current value as its label; click opens a page.</summary>
    Picker,
}

/// <summary>A quick-settings tile in the flyout grid.</summary>
public abstract partial class TileViewModel : ObservableObject
{
    private readonly List<Action> _unsubscribe = [];

    protected TileViewModel(string id, TileKind kind, string glyph, string name)
    {
        Id = id;
        Kind = kind;
        Glyph = glyph;
        Name = name;
        MainCommand = new RelayCommand(OnMain);
        SecondaryCommand = new RelayCommand(OnSecondary);
        RemoveCommand = new RelayCommand(() => RemoveRequested?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>The unpin badge was clicked in edit mode.</summary>
    public event EventHandler? RemoveRequested;

    /// <summary>Layout id, matching the design spec's tile catalog.</summary>
    public string Id { get; }

    public TileKind Kind { get; }

    /// <summary>Segoe Fluent Icons glyph, or <c>"HDR"</c> for the text glyph.</summary>
    public string Glyph { get; }

    /// <summary>Feature name, used for accessibility and as the label of non-picker tiles.</summary>
    public string Name { get; }

    public abstract string Label { get; }

    public abstract bool IsOn { get; }

    public abstract bool IsPending { get; }

    public abstract bool HasFailed { get; }

    /// <summary>Hidden when the monitor can't use the feature in its current mode.</summary>
    public abstract bool IsVisible { get; }

    /// <summary>In edit mode every tile shows, so it can be arranged even if it is hidden right now (for example in HDR).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsShown))]
    public partial bool IsEditing { get; set; }

    public bool IsShown => IsVisible || IsEditing;

    public IRelayCommand MainCommand { get; }

    public IRelayCommand SecondaryCommand { get; }

    public IRelayCommand RemoveCommand { get; }

    /// <summary>The monitor features this tile shows, re-read whenever the flyout opens.</summary>
    public abstract IEnumerable<Feature> Features { get; }

    protected abstract void OnMain();

    protected virtual void OnSecondary() => OnMain();

    /// <summary>Drops subscriptions to the (longer-lived) monitor state. Call when the tile leaves the flyout.</summary>
    public void Detach()
    {
        _unsubscribe.ForEach(unsubscribe => unsubscribe());
        _unsubscribe.Clear();
    }

    /// <summary>Re-raises change notifications for every computed property when a source changes.</summary>
    protected void Track(INotifyPropertyChanged source) => Observe(source, (_, _) => RaiseAll());

    protected void Observe(INotifyPropertyChanged source, PropertyChangedEventHandler handler)
    {
        source.PropertyChanged += handler;
        _unsubscribe.Add(() => source.PropertyChanged -= handler);
    }

    protected void RaiseAll()
    {
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(IsOn));
        OnPropertyChanged(nameof(IsPending));
        OnPropertyChanged(nameof(HasFailed));
        OnPropertyChanged(nameof(IsVisible));
        OnPropertyChanged(nameof(IsShown));
    }
}
