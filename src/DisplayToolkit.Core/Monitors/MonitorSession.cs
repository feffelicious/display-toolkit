using DisplayToolkit.Core.Capabilities;
using DisplayToolkit.Core.Ddc;
using DisplayToolkit.Core.Features;

namespace DisplayToolkit.Core.Monitors;

/// <summary>
/// A live connection to one monitor: what it supports, the last known value of every supported feature, and
/// serialized, verified reads and writes.
/// </summary>
/// <remarks>
/// All DDC/CI traffic runs on a per-monitor worker thread. <see cref="ValueChanged"/> is raised on that thread, so
/// UI code must marshal to its dispatcher.
/// </remarks>
public sealed class MonitorSession : IDisposable
{
    private readonly ResilientChannel _channel;
    private readonly DdcWorker _worker;
    private readonly MonitorSessionOptions _options;

    // State, guarded by _stateGate. _confirmed holds what the monitor last reported; _visible is what the UI should show
    // (the optimistic value while a write is pending). _latestWrite tracks the newest write per feature so that an
    // older write completing doesn't overwrite the optimistic value of a newer one.
    private readonly Lock _stateGate = new();
    private readonly Dictionary<string, FeatureValue> _confirmed = [];
    private readonly Dictionary<string, FeatureValue> _visible = [];
    private readonly Dictionary<string, long> _latestWrite = [];
    private long _writeSequence;

    private MonitorSession(MonitorConnection connection, IMonitorEnumerator enumerator, MonitorSessionOptions options)
    {
        Id = connection.Id;
        Name = connection.Name;
        _options = options;
        _channel = new ResilientChannel(connection, enumerator, options);
        _worker = new DdcWorker(connection.Name);
    }

    public MonitorId Id { get; }

    public string Name { get; }

    public MonitorCapabilities Capabilities { get; private set; } = MonitorCapabilities.Empty;

    /// <summary>The catalog features this monitor supports, in catalog order.</summary>
    public IReadOnlyList<Feature> Features { get; private set; } = [];

    /// <summary>Raised on the DDC worker thread whenever a feature's visible value or status changes.</summary>
    public event EventHandler<FeatureValueChangedEventArgs>? ValueChanged;

    /// <summary>
    /// Takes ownership of <paramref name="connection"/>, reads the capabilities (unless already known, which saves
    /// 1–2 s) and then the current value of every supported feature.
    /// </summary>
    public static async Task<MonitorSession> OpenAsync(
        MonitorConnection connection,
        IMonitorEnumerator enumerator,
        MonitorSessionOptions? options = null,
        MonitorCapabilities? knownCapabilities = null)
    {
        var session = new MonitorSession(connection, enumerator, options ?? MonitorSessionOptions.Default);
        try
        {
            var capabilities = knownCapabilities ?? await session._worker.Enqueue(session.ReadCapabilities);
            session.Capabilities = capabilities;
            session.Features = [.. FeatureCatalog.All.Where(feature => feature.IsSupportedBy(capabilities))];
            await session.RefreshAsync();
            return session;
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    public bool Supports(Feature feature) => Features.Contains(feature);

    public IReadOnlyList<FeatureOption> OptionsOf(EnumFeature feature) => feature.SupportedOptions(Capabilities);

    /// <summary>The value to show, or null if the feature is unsupported or hasn't been read yet.</summary>
    public FeatureValue? GetValue(Feature feature)
    {
        lock (_stateGate)
        {
            return _visible.GetValueOrDefault(feature.Id);
        }
    }

    /// <summary>
    /// Re-reads the given features (default: all supported), one DDC read per register. Use it when the window opens:
    /// the monitor doesn't report changes made from its own menu.
    /// </summary>
    public Task RefreshAsync(IEnumerable<Feature>? features = null)
    {
        var codes = (features ?? Features).Where(Supports).Select(feature => feature.Code).Distinct();
        return Task.WhenAll(codes.Select(ReadRegisterAsync));
    }

    public async Task<FeatureValue?> ReadAsync(Feature feature)
    {
        EnsureSupported(feature);
        await ReadRegisterAsync(feature.Code);
        return GetValue(feature);
    }

    /// <summary>
    /// Writes a value and confirms it by reading it back. Returns a <see cref="FeatureStatus.Confirmed"/> or
    /// <see cref="FeatureStatus.Failed"/> value; monitor errors are reported that way rather than thrown.
    /// </summary>
    /// <remarks>
    /// The new value becomes visible as <see cref="FeatureStatus.Pending"/> immediately. Rapid writes to the same
    /// feature coalesce, so only the newest queued value is sent.
    /// </remarks>
    public Task<FeatureValue> WriteAsync(Feature feature, uint value)
    {
        EnsureSupported(feature);

        long sequence;
        FeatureValue pending;
        lock (_stateGate)
        {
            sequence = ++_writeSequence;
            _latestWrite[feature.Id] = sequence;
            var maximum = _confirmed.GetValueOrDefault(feature.Id)?.Maximum ?? 0;
            pending = new FeatureValue(feature, value, maximum, FeatureStatus.Pending);
            _visible[feature.Id] = pending;
        }
        RaiseChanged([pending]);

        return _worker.Enqueue(() => ExecuteWrite(feature, value, sequence), $"write:{feature.Id}");
    }

    public void Dispose()
    {
        _worker.Dispose();
        _channel.Dispose();
    }

    /// <summary>Reads one register. A register that doesn't answer keeps its last known value and returns false.</summary>
    private Task<bool> ReadRegisterAsync(byte code) => _worker.Enqueue(() =>
    {
        try
        {
            Publish(code, _channel.Get(code), completedWrite: null);
            return true;
        }
        catch (DdcException)
        {
            return false;
        }
    }, $"read:{code:X2}");

    /// <summary>
    /// The capabilities reply is a long multi-packet transfer and fails far more often than a single read, especially
    /// when another program (such as ASUS DisplayWidget Center) is using the bus. It gets its own, more patient retries;
    /// a garbled reply without any VCP codes counts as a failure.
    /// </summary>
    private MonitorCapabilities ReadCapabilities()
    {
        DdcException? lastError = null;
        for (var attempt = 0; attempt < _options.CapabilitiesAttempts; attempt++)
        {
            if (attempt > 0)
            {
                Sleep(_options.CapabilitiesRetryDelay);
            }

            try
            {
                var capabilities = CapabilitiesParser.Parse(_channel.GetCapabilities());
                if (capabilities.VcpCodes.Any())
                {
                    return capabilities;
                }
            }
            catch (DdcException exception)
            {
                lastError = exception;
            }
        }
        throw new DdcException("The monitor didn't send a usable capabilities string.", lastError ?? new DdcException());
    }

    private FeatureValue ExecuteWrite(Feature feature, uint value, long sequence)
    {
        for (var attempt = 0; attempt <= _options.Retries; attempt++)
        {
            try
            {
                var register = feature.IsPartialRegister ? _channel.Get(feature.Code).Current : 0;
                _channel.Set(feature.Code, feature.Encode(value, register));

                if (Verify(feature, value) is { } reply)
                {
                    Publish(feature.Code, reply, (feature, sequence));
                    return new FeatureValue(feature, feature.Decode(reply), feature.DecodeMaximum(reply), FeatureStatus.Confirmed);
                }
            }
            catch (DdcException)
            {
                // The channel has already retried and tried reconnecting; retry the whole write.
            }
            Sleep(_options.RetryDelay);
        }
        return Fail(feature, sequence);
    }

    /// <summary>Reads the register back until it shows <paramref name="value"/>. Returns the matching reply, or null.</summary>
    private VcpReply? Verify(Feature feature, uint value)
    {
        if (feature.Settling == WriteSettling.Immediate)
        {
            Sleep(_options.VerifyDelay);
            var reply = _channel.Get(feature.Code);
            return feature.Decode(reply) == value ? reply : null;
        }

        // Mode switches: the monitor may disappear and come back, and reports stale values in the meantime.
        var deadline = DateTime.UtcNow + _options.ModeSwitchTimeout;
        do
        {
            Sleep(_options.ModeSwitchPollInterval);
            try
            {
                var reply = _channel.Get(feature.Code);
                if (feature.Decode(reply) == value)
                {
                    return reply;
                }
            }
            catch (DdcException)
            {
                // Still switching.
            }
        }
        while (DateTime.UtcNow < deadline);
        return null;
    }

    private FeatureValue Fail(Feature feature, long sequence)
    {
        FeatureValue failed;
        lock (_stateGate)
        {
            var confirmed = _confirmed.GetValueOrDefault(feature.Id);
            failed = new FeatureValue(feature, confirmed?.Value ?? 0, confirmed?.Maximum ?? 0, FeatureStatus.Failed);

            // If a newer write is queued, it decides what's visible.
            if (!IsLatestWrite(feature, sequence))
            {
                return failed;
            }
            _latestWrite.Remove(feature.Id);
            _visible[feature.Id] = failed;
        }
        RaiseChanged([failed]);
        return failed;
    }

    /// <summary>
    /// Records one register reply for every supported feature stored in <paramref name="code"/>, which matters for
    /// bitmask registers where one read refreshes many features.
    /// </summary>
    private void Publish(byte code, VcpReply reply, (Feature Feature, long Sequence)? completedWrite)
    {
        var changed = new List<FeatureValue>();
        lock (_stateGate)
        {
            if (completedWrite is var (written, sequence) && IsLatestWrite(written, sequence))
            {
                _latestWrite.Remove(written.Id);
            }

            foreach (var feature in Features.Where(feature => feature.Code == code))
            {
                var status = reply.IsLocked ? FeatureStatus.Locked : FeatureStatus.Confirmed;
                var value = new FeatureValue(feature, feature.Decode(reply), feature.DecodeMaximum(reply), status);
                _confirmed[feature.Id] = value;

                // Keep showing the optimistic value while a write for this feature is still in flight.
                if (!_latestWrite.ContainsKey(feature.Id) && _visible.GetValueOrDefault(feature.Id) != value)
                {
                    _visible[feature.Id] = value;
                    changed.Add(value);
                }
            }
        }
        RaiseChanged(changed);
    }

    private bool IsLatestWrite(Feature feature, long sequence) =>
        _latestWrite.TryGetValue(feature.Id, out var latest) && latest == sequence;

    private void RaiseChanged(IEnumerable<FeatureValue> values)
    {
        foreach (var value in values)
        {
            ValueChanged?.Invoke(this, new FeatureValueChangedEventArgs(value));
        }
    }

    private void EnsureSupported(Feature feature)
    {
        if (!Supports(feature))
        {
            throw new NotSupportedException($"{Name} doesn't support {feature}.");
        }
    }

    private static void Sleep(TimeSpan duration)
    {
        if (duration > TimeSpan.Zero)
        {
            Thread.Sleep(duration);
        }
    }
}
