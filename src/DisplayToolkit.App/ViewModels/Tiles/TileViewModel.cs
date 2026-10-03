using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
    protected TileViewModel(string id, TileKind kind, string glyph, string name)
    {
        Id = id;
        Kind = kind;
        Glyph = glyph;
        Name = name;
        MainCommand = new RelayCommand(OnMain);
        SecondaryCommand = new RelayCommand(OnSecondary);
    }

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

    public IRelayCommand MainCommand { get; }

    public IRelayCommand SecondaryCommand { get; }

    protected abstract void OnMain();

    protected virtual void OnSecondary() => OnMain();

    /// <summary>Re-raises change notifications for every computed property when a source changes.</summary>
    protected void Track(INotifyPropertyChanged source) => source.PropertyChanged += (_, _) => RaiseAll();

    protected void RaiseAll()
    {
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(IsOn));
        OnPropertyChanged(nameof(IsPending));
        OnPropertyChanged(nameof(HasFailed));
        OnPropertyChanged(nameof(IsVisible));
    }
}
