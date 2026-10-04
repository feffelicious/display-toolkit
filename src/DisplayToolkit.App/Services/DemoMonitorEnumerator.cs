#if DEBUG
using DisplayToolkit.Core.Ddc;
using DisplayToolkit.Core.Monitors;

namespace DisplayToolkit.App.Services;

/// <summary>
/// Debug builds only: with <c>DISPLAYTOOLKIT_DEMO_MONITOR=1</c>, adds an in-memory monitor after the real ones, for
/// trying the monitor switch with a single screen. It has brightness, contrast, volume and inputs, and remembers what
/// is written until the app exits.
/// </summary>
internal sealed class DemoMonitorEnumerator(IMonitorEnumerator real) : IMonitorEnumerator
{
    private static readonly MonitorId DemoId = new("DEM0001", @"\\?\DISPLAY#DEMO#0");
    private static readonly DemoChannel.Registers Values = new();

    public static bool IsRequested => Environment.GetEnvironmentVariable("DISPLAYTOOLKIT_DEMO_MONITOR") == "1";

    public IReadOnlyList<MonitorConnection> Enumerate() => [.. real.Enumerate(), new MonitorConnection(DemoId, "Demo monitor", new DemoChannel(Values))];

    private sealed class DemoChannel(DemoChannel.Registers registers) : IDdcChannel
    {
        public string GetCapabilities() =>
            "(prot(monitor)type(LCD)model(Demo)cmds(01 02 03 07 0C F3)vcp(02 10 12 60(0F 11 12) 62 D6(01 05))mccs_ver(2.2))";

        public VcpReply Get(byte code)
        {
            lock (registers)
            {
                return registers.TryGetValue(code, out var value) ? new VcpReply(value, 100) : new VcpReply(code == 0x60 ? 0x0Fu : 50, 100);
            }
        }

        public void Set(byte code, uint value)
        {
            lock (registers)
            {
                registers[code] = value;
            }
        }

        public void Dispose()
        {
        }

        public sealed class Registers : Dictionary<byte, uint>;
    }
}
#endif
