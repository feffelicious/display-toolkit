// Display Toolkit probe: lists every monitor, its capabilities and the value of every feature Display Toolkit supports.
// Read-only. Attach the output to bug reports and to requests for new monitor models.
//
//   --all    also reads every VCP code the monitor advertises, including ones Display Toolkit doesn't know yet.
//   --watch  like --all, then waits: change one setting in the monitor's own menu, press Enter, and the probe lists
//            the codes whose value changed. That's how a new setting's code is found.

using DisplayToolkit.Core;
using DisplayToolkit.Core.Ddc;
using DisplayToolkit.Core.Features;
using DisplayToolkit.Core.Monitors;

var watch = args.Contains("--watch");
var all = watch || args.Contains("--all");

var enumerator = new Win32MonitorEnumerator();
var connections = enumerator.Enumerate();
Console.WriteLine($"Found {connections.Count} monitor(s).");

var sessions = new List<MonitorSession>();
var snapshots = new Dictionary<MonitorSession, Dictionary<byte, VcpReply?>>();

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

    sessions.Add(session);
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

    if (all)
    {
        Console.WriteLine();
        Console.WriteLine("  Every advertised code:");
        var snapshot = await ReadAllAsync(session);
        snapshots[session] = snapshot;
        foreach (var (code, reply) in snapshot)
        {
            Console.WriteLine($"  0x{code:X2}  {DescribeRaw(reply)}");
        }
    }
}

if (watch && sessions.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Change one setting with the monitor's own buttons, then press Enter here. Type q and Enter to quit.");
    while (Console.ReadLine() is { } line && !line.Trim().Equals("q", StringComparison.OrdinalIgnoreCase))
    {
        foreach (var session in sessions)
        {
            var before = snapshots[session];
            var after = await ReadAllAsync(session);
            var changed = after.Where(entry => before.GetValueOrDefault(entry.Key) != entry.Value).ToList();

            Console.WriteLine($"=== {session.Name}: {(changed.Count == 0 ? "nothing changed" : $"{changed.Count} code(s) changed")}");
            foreach (var (code, reply) in changed)
            {
                Console.WriteLine($"  0x{code:X2}  {DescribeRaw(before.GetValueOrDefault(code))}  ->  {DescribeRaw(reply)}");
            }

            snapshots[session] = after;
        }

        Console.WriteLine();
        Console.WriteLine("Change the next setting and press Enter, or type q to quit.");
    }
}

foreach (var session in sessions)
{
    session.Dispose();
}

static async Task<Dictionary<byte, VcpReply?>> ReadAllAsync(MonitorSession session)
{
    var replies = new Dictionary<byte, VcpReply?>();

    // 0xE8 (GamePlus overlay position) is a move command that never answers reads; asking only costs retries.
    foreach (var code in session.Capabilities.VcpCodes.Where(code => code != Vcp.AsusGamePlusPosition))
    {
        replies[code] = await session.ReadRawAsync(code);
    }

    return replies;
}

static string DescribeRaw(VcpReply? reply) => reply is { } value
    ? $"current 0x{value.Current:X4} ({value.Current}), max 0x{value.Maximum:X4} ({value.Maximum})"
    : "(no reply)";

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
