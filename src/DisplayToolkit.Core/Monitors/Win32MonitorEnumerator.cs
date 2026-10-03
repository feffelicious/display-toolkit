using DisplayToolkit.Core.Ddc;
using DisplayToolkit.Core.Native;

namespace DisplayToolkit.Core.Monitors;

/// <summary>
/// Finds physical monitors through the Monitor Configuration API and identifies them through the CCD API, which
/// exposes EDID ids and stable device paths.
/// </summary>
public sealed class Win32MonitorEnumerator : IMonitorEnumerator
{
    public IReadOnlyList<MonitorConnection> Enumerate()
    {
        var targetsBySource = GetTargetsBySourceName();
        var connections = new List<MonitorConnection>();

        foreach (var hMonitor in User32.GetMonitorHandles())
        {
            var gdiName = User32.GetDeviceName(hMonitor) ?? string.Empty;
            var targets = targetsBySource.GetValueOrDefault(gdiName) ?? [];
            var physicalMonitors = GetPhysicalMonitors(hMonitor);

            // In clone mode one HMONITOR has several physical monitors; both lists follow the same order.
            for (var i = 0; i < physicalMonitors.Count; i++)
            {
                var (handle, description) = physicalMonitors[i];
                var target = i < targets.Count ? targets[i] : null;
                if (target?.IsInternal == true)
                {
                    new Dxva2DdcChannel(handle).Dispose();
                    continue;
                }

                var id = target?.Id ?? new MonitorId("UNKNOWN", $"{gdiName}#{i}");
                var name = string.IsNullOrWhiteSpace(target?.FriendlyName) ? description : target.FriendlyName;
                connections.Add(new MonitorConnection(id, name, new Dxva2DdcChannel(handle)));
            }
        }
        return connections;
    }

    private sealed record TargetInfo(MonitorId Id, string FriendlyName, bool IsInternal);

    /// <summary>
    /// Laptop panels (LVDS, embedded DisplayPort/UDI, "internal") never speak DDC/CI, and trying them costs seconds of
    /// retries on every rescan.
    /// </summary>
    private static bool IsInternalConnection(uint outputTechnology) => outputTechnology is 6 or 11 or 13 or 0x80000000;

    private static Dictionary<string, List<TargetInfo>> GetTargetsBySourceName()
    {
        var result = new Dictionary<string, List<TargetInfo>>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in DisplayConfig.GetActivePaths())
        {
            if (DisplayConfig.GetSourceGdiName(path) is not { } sourceName || DisplayConfig.GetTargetName(path) is not { } target)
            {
                continue;
            }

            var model = EdidModel(target.EdidManufactureId, target.EdidProductCodeId);
            if (!result.TryGetValue(sourceName, out var targets))
            {
                result[sourceName] = targets = [];
            }
            targets.Add(new TargetInfo(new MonitorId(model, target.DevicePath), target.FriendlyName, IsInternalConnection(target.OutputTechnology)));
        }
        return result;
    }

    private static unsafe List<(nint Handle, string Description)> GetPhysicalMonitors(nint hMonitor)
    {
        if (!Dxva2.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out var count) || count == 0)
        {
            return [];
        }

        var monitors = new Dxva2.PhysicalMonitor[count];
        fixed (Dxva2.PhysicalMonitor* monitorsPtr = monitors)
        {
            if (!Dxva2.GetPhysicalMonitorsFromHMONITOR(hMonitor, count, monitorsPtr))
            {
                return [];
            }

            var result = new List<(nint, string)>((int)count);
            for (var i = 0; i < count; i++)
            {
                result.Add((monitorsPtr[i].Handle, new string(monitorsPtr[i].Description)));
            }
            return result;
        }
    }

    /// <summary>
    /// Formats the EDID ids as the familiar PnP model string, for example <c>AUS32B1</c>. The manufacturer id is three
    /// 5-bit letters stored big-endian; the CCD API returns it byte-swapped.
    /// </summary>
    internal static string EdidModel(ushort manufacturerId, ushort productCode)
    {
        var id = (ushort)((manufacturerId >> 8) | (manufacturerId << 8));
        var letters = new[]
        {
            (char)('A' - 1 + ((id >> 10) & 0x1F)),
            (char)('A' - 1 + ((id >> 5) & 0x1F)),
            (char)('A' - 1 + (id & 0x1F)),
        };
        return $"{new string(letters)}{productCode:X4}";
    }
}
