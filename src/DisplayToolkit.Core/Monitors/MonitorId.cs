namespace DisplayToolkit.Core.Monitors;

/// <summary>Stable identity of a physical monitor across reboots, reconnects and handle changes.</summary>
/// <param name="Model">EDID manufacturer and product code, for example <c>AUS32B1</c>. Same for every unit of a model.</param>
/// <param name="Instance">The Windows device path of this unit on this connector.</param>
public sealed record MonitorId(string Model, string Instance)
{
    public override string ToString() => $"{Model} ({Instance})";
}
