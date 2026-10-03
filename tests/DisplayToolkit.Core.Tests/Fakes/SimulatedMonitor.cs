using DisplayToolkit.Core.Ddc;
using DisplayToolkit.Core.Monitors;

namespace DisplayToolkit.Core.Tests.Fakes;

/// <summary>
/// An in-memory monitor that behaves like the PG32UCWM we measured: registers with maximums, bitmask registers,
/// locked (0xFE) settings, and failure modes such as dead handles and transient values after a mode switch.
/// </summary>
internal sealed class SimulatedMonitor
{
    public const string Pg32ucwmCapabilities =
        "(prot(monitor)type(LCD)model(PG32UCWM)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 08 10 12 14(03 04 05 06 07 08 09 0B) " +
        "16 18 1A 47 52 59 5A 5B 5C 5D 5E 5F(0001) 60(0F 11 12 1A) 61(01 02 03) 62 72(5000 6400 7800 8C00 A000) 87 8A 8D(01 02) " +
        "A8 AC AE B2 B6 C0 C5(00 01) C6 C8 CC(01 02 03 04 05 06 07 08 09 0A 0C 0D 11 12 14 1A 1E 1F 23 24 25 26 27) D1(00 01 02 03) " +
        "D6(01 05) DC(0000 0100 0200 01 02 04 05 06 07 08 09 0A) DF E1(00 01) E2(0000 0100 0200 01 02 03 04 05 06 07) " +
        "E3(00 07 08 09 0A 0B 0C 0D 0E 0F) E4(00 01 02 03 04 05) E5(00 01 02 03 04) E6 E7(00 01) E8(01 02 03 04 05 06 07 08) E9 " +
        "EA(00 01 02) EB(00 01 02 03 04 05 06 07 08 0A 0B 0D 0E 0F) EC(01 FF) ED(0500 0A00 0F00 00 01 02 03 FF) EE(00 01) EF " +
        "F2(0000 0100 0200 0300 0400 0500 0600 00 01 02 03 04 05 06) F3(02 03 04) F4(00 01 02 03 04 05) F5(0F00 1100 1200 1A00 0F 11 12 1A) " +
        "F6(0000 0100 0200 01 02 04 05 06 07 08 09 0A) F7(0000 0100 0200 0300 0400 00 01 02 03) F8(00 02 04 08) F9 FA(01 02 03 04) " +
        "FB(0100 0200 0300 0400 0500 0600 0700 0800 0900 0A00 0B00 0C00 0E00 0F00 1000 1100 01 02 03 04 05 06 07 08 09 0A 0B 0C 0E 0F 10 11) " +
        "FC(089F) FD(6979) FF)mswhql(1)mccs_ver(2.2)asset_eep(40)";

    private readonly Lock _gate = new();
    private readonly Dictionary<byte, VcpReply> _registers = [];
    private readonly Dictionary<byte, int> _staleReadsAfterWrite = [];
    private readonly Dictionary<byte, int> _staleReadsRemaining = [];
    private readonly HashSet<byte> _ignoredWrites = [];
    private readonly HashSet<byte> _reconnectAfterWrite = [];
    private int _generation;

    public SimulatedMonitor(string capabilities = Pg32ucwmCapabilities)
    {
        Capabilities = capabilities;
    }

    /// <summary>Starts with the PG32UCWM values read during research (SDR, User mode).</summary>
    public static SimulatedMonitor Pg32ucwm()
    {
        var monitor = new SimulatedMonitor();
        monitor.SetRegister(Vcp.Brightness, 70, 100);
        monitor.SetRegister(Vcp.Contrast, 80, 100);
        monitor.SetRegister(Vcp.ColorPreset, 11, 11);
        monitor.SetRegister(Vcp.Gamma, 0x7800, 0xA000);
        monitor.SetRegister(Vcp.AsusGameVisual, 4, 0x020A);
        monitor.SetRegister(Vcp.AsusHdrMode, 0, 0x0207);
        monitor.SetRegister(Vcp.AsusShadowBoost, 0, 4);
        monitor.SetRegister(Vcp.AsusBlueLightFilter, 0, 4);
        monitor.SetRegister(Vcp.AsusCrosshair, 0, 0x0F);
        monitor.SetRegister(Vcp.AsusProximitySensor, 0x0501, 0x0FFF);
        monitor.SetRegister(Vcp.AsusToggles1, 0x0811, 0x089F);
        monitor.SetRegister(Vcp.AsusToggles2, 0x6829, 0x6979);
        return monitor;
    }

    public string Capabilities { get; }

    public int SetCount { get; private set; }

    public List<(byte Code, uint Value)> Writes { get; } = [];

    /// <summary>Called inside every Set, before it takes effect. Lets tests block the worker mid-write.</summary>
    public Action<byte, uint>? OnSet { get; set; }

    public void SetRegister(byte code, uint current, uint maximum)
    {
        lock (_gate)
        {
            _registers[code] = new VcpReply(current, maximum);
        }
    }

    public uint Current(byte code)
    {
        lock (_gate)
        {
            return _registers[code].Current;
        }
    }

    /// <summary>Makes every existing channel fail, like Windows re-enumerating the display after an HDR400 switch.</summary>
    public void InvalidateHandles()
    {
        lock (_gate)
        {
            _generation++;
        }
    }

    /// <summary>After the next write to <paramref name="code"/>, the next <paramref name="reads"/> reads return 0.</summary>
    public void ReportStaleValuesAfterWrite(byte code, int reads)
    {
        lock (_gate)
        {
            _staleReadsAfterWrite[code] = reads;
        }
    }

    /// <summary>
    /// A write to <paramref name="code"/> takes effect and then invalidates every handle, like VRR or anti-flicker making
    /// Windows re-detect the display.
    /// </summary>
    public void ReconnectAfterWrite(byte code)
    {
        lock (_gate)
        {
            _reconnectAfterWrite.Add(code);
        }
    }

    /// <summary>Writes to <paramref name="code"/> are acknowledged but have no effect.</summary>
    public void IgnoreWrites(byte code)
    {
        lock (_gate)
        {
            _ignoredWrites.Add(code);
        }
    }

    public IDdcChannel OpenChannel()
    {
        lock (_gate)
        {
            return new Channel(this, _generation);
        }
    }

    public IMonitorEnumerator Enumerator(MonitorId id) => new SingleMonitorEnumerator(this, id);

    private VcpReply Get(int generation, byte code)
    {
        lock (_gate)
        {
            ThrowIfStale(generation);
            if (!_registers.TryGetValue(code, out var reply))
            {
                reply = new VcpReply(0, 100);
            }
            if (_staleReadsRemaining.TryGetValue(code, out var stale) && stale > 0)
            {
                _staleReadsRemaining[code] = stale - 1;
                return reply with { Current = 0 };
            }
            return reply;
        }
    }

    private void Set(int generation, byte code, uint value)
    {
        OnSet?.Invoke(code, value);
        lock (_gate)
        {
            ThrowIfStale(generation);
            SetCount++;
            Writes.Add((code, value));
            if (_ignoredWrites.Contains(code))
            {
                return;
            }
            var maximum = _registers.TryGetValue(code, out var existing) ? existing.Maximum : 0xFFFF;
            _registers[code] = new VcpReply(value, maximum);
            if (_staleReadsAfterWrite.Remove(code, out var staleReads))
            {
                _staleReadsRemaining[code] = staleReads;
            }
            if (_reconnectAfterWrite.Contains(code))
            {
                _generation++;
            }
        }
    }

    private void ThrowIfStale(int generation)
    {
        if (generation != _generation)
        {
            throw new DdcException("Stale handle", unchecked((int)0xC026258C));
        }
    }

    private sealed class Channel(SimulatedMonitor monitor, int generation) : IDdcChannel
    {
        public string GetCapabilities() => monitor.Capabilities;

        public VcpReply Get(byte code) => monitor.Get(generation, code);

        public void Set(byte code, uint value) => monitor.Set(generation, code, value);

        public void Dispose()
        {
        }
    }

    private sealed class SingleMonitorEnumerator(SimulatedMonitor monitor, MonitorId id) : IMonitorEnumerator
    {
        public IReadOnlyList<MonitorConnection> Enumerate() => [new MonitorConnection(id, "PG32UCWM", monitor.OpenChannel())];
    }
}
