// Display Toolkit probe: lists every monitor, its capabilities and the value of every feature Display Toolkit supports.
// Read-only. Attach the output to bug reports and to requests for new monitor models.

using DisplayToolkit.Core.Features;
using DisplayToolkit.Core.Monitors;

var enumerator = new Win32MonitorEnumerator();
var connections = enumerator.Enumerate();
Console.WriteLine($"Found {connections.Count} monitor(s).");

foreach (var connection in connections)
{
    Console.WriteLine();
    Console.WriteLine($"=== {connection.Name}");
    Console.WriteLine($"Model:    {connection.Id.Model}");
    Console.WriteLine($"Instance: {connection.Id.Instance}");
    Console.WriteLine($"Windows HDR: {(WindowsHdr.GetState(connection.Id) is { } hdr ? $"supported={hdr.IsSupported}, on={hdr.IsEnabled}" : "unknown")}");

    MonitorSession session;
    try
    {
        session = await MonitorSession.OpenAsync(connection, enumerator);
    }
    catch (Exception exception)
    {
        Console.WriteLine($"Can't talk to this monitor over DDC/CI: {exception.Message}");
        continue;
    }

    using (session)
    {
        Console.WriteLine($"Capabilities: {session.Capabilities.Raw}");
        Console.WriteLine();

        foreach (var feature in session.Features)
        {
            var value = session.GetValue(feature);
            var text = value is null ? "(no reply)" : Describe(feature, value);
            Console.WriteLine($"  {feature.Id,-24} 0x{feature.Code:X2}  {text}");
        }

        var unsupported = FeatureCatalog.All.Except(session.Features).Select(feature => feature.Id).ToList();
        if (unsupported.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"  Not supported: {string.Join(", ", unsupported)}");
        }
    }
}

static string Describe(Feature feature, FeatureValue value)
{
    var status = value.Status == FeatureStatus.Confirmed ? string.Empty : $"  [{value.Status}]";
    return feature switch
    {
        EnumFeature enumFeature => $"{enumFeature.FindOption(value.Value)?.Name ?? $"0x{value.Value:X}"}{status}",
        FlagFeature => $"{(value.Value != 0 ? "on" : "off")}{status}",
        _ => $"{value.Value} / {value.Maximum}{status}",
    };
}
