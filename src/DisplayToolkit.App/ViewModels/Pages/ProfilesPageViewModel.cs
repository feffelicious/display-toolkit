using DisplayToolkit.App.ViewModels.Automation;

namespace DisplayToolkit.App.ViewModels.Pages;

/// <summary>Every quick settings profile, for when they don't all fit in the profiles row.</summary>
public sealed class ProfilesPageViewModel(IReadOnlyList<ProfileChipViewModel> profiles) : PageViewModel("Profiles")
{
    public IReadOnlyList<ProfileChipViewModel> Profiles { get; } = profiles;
}
