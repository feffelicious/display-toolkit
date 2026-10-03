using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DisplayToolkit.Automation.Profiles;

namespace DisplayToolkit.App.ViewModels.Automation;

/// <summary>A profile in the quick settings row, the flyout's profile list or the tray menu.</summary>
public sealed partial class ProfileChipViewModel(Profile profile, Func<Profile, Task> apply) : ObservableObject
{
    public Profile Profile { get; private set; } = profile;

    public string Name => Profile.Name;

    public string Glyph => Profile.Glyph;

    /// <summary>The profile in use.</summary>
    [ObservableProperty]
    public partial bool IsActive { get; set; }

    /// <summary>Being written to the monitor.</summary>
    [ObservableProperty]
    public partial bool IsPending { get; set; }

    /// <summary>Picks up edits (a new name or glyph) without replacing the chip, which would lose keyboard focus.</summary>
    public void Update(Profile updated)
    {
        Profile = updated;
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Glyph));
    }

    [RelayCommand]
    private Task Select() => apply(Profile);
}
