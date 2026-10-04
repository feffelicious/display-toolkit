using DisplayToolkit.App.Services;

namespace DisplayToolkit.App.ViewModels.Pages;

/// <summary>Picks the monitor quick settings controls, when several are connected.</summary>
public sealed class MonitorsPageViewModel : PageViewModel
{
    internal MonitorsPageViewModel(MonitorContext context)
        : base("Monitor")
    {
        var monitors = context.Monitors;
        Options = [.. monitors.Select((monitor, index) =>
            new OptionItemViewModel(context.NameOf(monitor), (uint)index, value => context.Select(monitors[(int)value]))
            {
                IsSelected = context.Current?.Session == monitor,
            })];
    }

    public IReadOnlyList<OptionItemViewModel> Options { get; }
}
