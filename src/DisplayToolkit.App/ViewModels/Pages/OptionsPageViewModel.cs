using DisplayToolkit.Core.Features;

namespace DisplayToolkit.App.ViewModels.Pages;

/// <summary>Pick one of a feature's options. Selecting applies immediately and stays on the page.</summary>
public sealed class OptionsPageViewModel : PageViewModel
{
    private readonly FeatureState _state;

    public OptionsPageViewModel(string title, FeatureState state, IEnumerable<FeatureOption> options)
        : base(title)
    {
        _state = state;
        Options = [.. options.Select(option => new OptionItemViewModel(option.Name, option.Value, state.Write))];
        Observe(state, UpdateSelection);
        UpdateSelection();
    }

    public IReadOnlyList<OptionItemViewModel> Options { get; }

    private void UpdateSelection()
    {
        foreach (var option in Options)
        {
            option.IsSelected = option.Value == _state.Value;
        }
    }
}
