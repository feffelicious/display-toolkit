namespace DisplayToolkit.App.ViewModels.Pages;

/// <summary>An on/off switch plus a level from 1 to the monitor's maximum (blue light filter, shadow boost).</summary>
public sealed class LevelsPageViewModel : PageViewModel
{
    private readonly FeatureState _state;
    private uint _lastLevel;

    public LevelsPageViewModel(FeatureState state, string? note = null)
        : base(state.Feature.Name)
    {
        _state = state;
        Note = note;
        _lastLevel = state.Value != 0 ? state.Value : 1;
        Levels = [.. Enumerable.Range(1, (int)Math.Max(1, state.Maximum))
            .Select(level => new OptionItemViewModel(level.ToString(System.Globalization.CultureInfo.CurrentCulture), (uint)level, state.Write))];
        Observe(state, Update);
        Update();
    }

    public string? Note { get; }

    public IReadOnlyList<OptionItemViewModel> Levels { get; }

    public bool IsOn
    {
        get => _state.IsOn;
        set => _state.Write(value ? _lastLevel : 0);
    }

    private void Update()
    {
        if (_state.Value != 0)
        {
            _lastLevel = _state.Value;
        }
        foreach (var level in Levels)
        {
            level.IsSelected = level.Value == _state.Value;
        }
        OnPropertyChanged(nameof(IsOn));
    }
}
