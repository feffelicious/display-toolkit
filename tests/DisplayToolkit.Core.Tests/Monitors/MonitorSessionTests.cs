using DisplayToolkit.Core.Features;
using DisplayToolkit.Core.Monitors;
using DisplayToolkit.Core.Tests.Fakes;

namespace DisplayToolkit.Core.Tests.Monitors;

public sealed class MonitorSessionTests : IDisposable
{
    private static readonly MonitorId Id = new("AUS32B1", @"\\?\DISPLAY#AUS32B1#test");

    private readonly SimulatedMonitor _monitor = SimulatedMonitor.Pg32ucwm();
    private MonitorSession? _session;

    public void Dispose() => _session?.Dispose();

    [Fact]
    public async Task Opening_reads_capabilities_and_current_values()
    {
        var session = await OpenAsync();

        Assert.Equal("PG32UCWM", session.Capabilities.Model);
        Assert.Contains(FeatureCatalog.Brightness, session.Features);
        Assert.DoesNotContain(FeatureCatalog.BoundaryDetection, session.Features);
        Assert.Equal(new FeatureValue(FeatureCatalog.Brightness, 70, 100, FeatureStatus.Confirmed), session.GetValue(FeatureCatalog.Brightness));
        Assert.Equal(1u, session.GetValue(FeatureCatalog.TaskbarDetection)!.Value);
        Assert.Equal(0u, session.GetValue(FeatureCatalog.UniformBrightness)!.Value);
    }

    [Fact]
    public async Task Write_is_confirmed_by_reading_back()
    {
        var session = await OpenAsync();

        var result = await session.WriteAsync(FeatureCatalog.Brightness, 40);

        Assert.Equal(FeatureStatus.Confirmed, result.Status);
        Assert.Equal(40u, _monitor.Current(Vcp.Brightness));
        Assert.Equal(result, session.GetValue(FeatureCatalog.Brightness));
    }

    [Fact]
    public async Task Resetting_the_mode_sends_the_reset_and_reads_the_factory_values()
    {
        var session = await OpenAsync();
        _monitor.OnSet = (code, _) =>
        {
            if (code == Vcp.AsusResetMode)
            {
                _monitor.SetRegister(Vcp.Brightness, 80, 100);
            }
        };

        Assert.True(session.CanResetCurrentMode);
        Assert.True(await session.ResetCurrentModeAsync());

        Assert.Contains((Vcp.AsusResetMode, 1u), _monitor.Writes);
        Assert.Equal(80u, session.GetValue(FeatureCatalog.Brightness)!.Value);
    }

    [Fact]
    public async Task Picture_mode_keeps_the_color_gamut()
    {
        _monitor.SetRegister(Vcp.AsusGameVisual, 0x0105, 0x020A);
        var session = await OpenAsync();

        Assert.Equal(5u, session.GetValue(FeatureCatalog.PictureMode)!.Value);
        Assert.Equal(1u, session.GetValue(FeatureCatalog.ColorGamut)!.Value);

        await session.WriteAsync(FeatureCatalog.PictureMode, 4);

        Assert.Equal(0x0104u, _monitor.Current(Vcp.AsusGameVisual));
        Assert.Equal(1u, session.GetValue(FeatureCatalog.ColorGamut)!.Value);
    }

    [Fact]
    public async Task Leaving_aura_sync_goes_through_off()
    {
        _monitor.SetRegister(Vcp.AsusAura, 0x0001, 0x0606);
        var session = await OpenAsync();

        await session.WriteAsync(FeatureCatalog.AuraEffect, 2);

        Assert.Equal([(Vcp.AsusAura, 0u), (Vcp.AsusAura, 2u)], _monitor.Writes);
        Assert.Equal(2u, session.GetValue(FeatureCatalog.AuraEffect)!.Value);
    }

    [Fact]
    public async Task Aura_color_changes_only_its_byte()
    {
        _monitor.SetRegister(Vcp.AsusAura, 0x0104, 0x0606);
        var session = await OpenAsync();

        await session.WriteAsync(FeatureCatalog.AuraColor, 3);

        Assert.Equal(0x0304u, _monitor.Current(Vcp.AsusAura));
        Assert.Equal(4u, session.GetValue(FeatureCatalog.AuraEffect)!.Value);
        Assert.Equal(3u, session.GetValue(FeatureCatalog.AuraColor)!.Value);
    }

    [Fact]
    public async Task Menu_keys_are_sent_as_is()
    {
        var session = await OpenAsync();

        Assert.True(session.CanPressMenuKeys);
        Assert.True(await session.PressMenuKeyAsync(MenuKey.Down));

        Assert.Equal([(Vcp.AsusEzOsd, 3u)], _monitor.Writes);
    }

    [Fact]
    public async Task Write_shows_pending_value_immediately()
    {
        var session = await OpenAsync();
        var seen = new List<FeatureValue>();
        session.ValueChanged += (_, e) => seen.Add(e.Value);

        await session.WriteAsync(FeatureCatalog.Brightness, 40);

        Assert.Equal(FeatureStatus.Pending, seen[0].Status);
        Assert.Equal(40u, seen[0].Value);
        Assert.Equal(FeatureStatus.Confirmed, seen[^1].Status);
    }

    [Fact]
    public async Task Flag_write_changes_only_its_bit()
    {
        var session = await OpenAsync();

        await session.WriteAsync(FeatureCatalog.UniformBrightness, 1);

        Assert.Equal(0x6869u, _monitor.Current(Vcp.AsusToggles2));
        Assert.Equal(1u, session.GetValue(FeatureCatalog.UniformBrightness)!.Value);
        Assert.Equal(1u, session.GetValue(FeatureCatalog.TaskbarDetection)!.Value);
    }

    [Fact]
    public async Task Locked_settings_are_reported_as_locked()
    {
        _monitor.SetRegister(Vcp.AsusShadowBoost, 0xFE, 0xFE);

        var session = await OpenAsync();

        Assert.Equal(FeatureStatus.Locked, session.GetValue(FeatureCatalog.ShadowBoost)!.Status);
    }

    [Fact]
    public async Task Settings_unavailable_in_hdr_are_locked_while_an_hdr_preset_is_active()
    {
        // In HDR the PG32UCWM still reports brightness normally, but ignores writes to it.
        _monitor.SetRegister(Vcp.AsusHdrMode, 0x0102, 0x0207);
        _monitor.SetRegister(Vcp.Brightness, 100, 100);

        var session = await OpenAsync();

        Assert.Equal(FeatureStatus.Locked, session.GetValue(FeatureCatalog.Brightness)!.Status);
        Assert.Equal(FeatureStatus.Locked, session.GetValue(FeatureCatalog.PictureMode)!.Status);
        Assert.Equal(FeatureStatus.Confirmed, session.GetValue(FeatureCatalog.Contrast)!.Status);
    }

    [Fact]
    public async Task Leaving_hdr_unlocks_settings()
    {
        _monitor.SetRegister(Vcp.AsusHdrMode, 0x0102, 0x0207);
        var session = await OpenAsync();

        _monitor.SetRegister(Vcp.AsusHdrMode, 0, 0x0207);
        await session.RefreshAsync([FeatureCatalog.HdrMode]);

        Assert.Equal(new FeatureValue(FeatureCatalog.Brightness, 70, 100, FeatureStatus.Confirmed), session.GetValue(FeatureCatalog.Brightness));
    }

    [Fact]
    public async Task Write_that_does_not_take_effect_fails_and_reverts()
    {
        var session = await OpenAsync();
        _monitor.IgnoreWrites(Vcp.Brightness);

        var result = await session.WriteAsync(FeatureCatalog.Brightness, 40);

        Assert.Equal(FeatureStatus.Failed, result.Status);
        Assert.Equal(70u, result.Value);
        Assert.Equal(result, session.GetValue(FeatureCatalog.Brightness));
    }

    [Fact]
    public async Task Rapid_writes_coalesce_to_the_latest_value()
    {
        var session = await OpenAsync();
        using var firstWriteStarted = new ManualResetEventSlim();
        using var releaseFirstWrite = new ManualResetEventSlim();
        _monitor.OnSet = (_, _) =>
        {
            if (!firstWriteStarted.IsSet)
            {
                firstWriteStarted.Set();
                releaseFirstWrite.Wait(TimeSpan.FromSeconds(5));
            }
        };

        var first = session.WriteAsync(FeatureCatalog.Brightness, 10);
        firstWriteStarted.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var queued = new List<Task<FeatureValue>>();
        for (var value = 20u; value <= 40; value += 10)
        {
            queued.Add(session.WriteAsync(FeatureCatalog.Brightness, value));
        }
        releaseFirstWrite.Set();
        await Task.WhenAll([first, .. queued]);

        Assert.Equal([(Vcp.Brightness, 10u), (Vcp.Brightness, 40u)], _monitor.Writes);
        Assert.Equal(40u, session.GetValue(FeatureCatalog.Brightness)!.Value);
        Assert.All(queued, task => Assert.Equal(40u, task.Result.Value));
    }

    [Fact]
    public async Task Older_write_completing_does_not_hide_a_newer_pending_value()
    {
        var session = await OpenAsync();
        using var releaseFirstWrite = new ManualResetEventSlim();
        _monitor.OnSet = (_, value) =>
        {
            if (value == 10)
            {
                releaseFirstWrite.Wait(TimeSpan.FromSeconds(5));
            }
        };
        var seen = new List<FeatureValue>();
        session.ValueChanged += (_, e) => seen.Add(e.Value);

        var first = session.WriteAsync(FeatureCatalog.Brightness, 10);
        var second = session.WriteAsync(FeatureCatalog.Brightness, 20);
        releaseFirstWrite.Set();
        await Task.WhenAll(first, second);

        // The UI never jumps back to 10 after the user moved on to 20.
        Assert.DoesNotContain(seen, value => value is { Value: 10, Status: FeatureStatus.Confirmed });
        Assert.Equal(new FeatureValue(FeatureCatalog.Brightness, 20, 100, FeatureStatus.Confirmed), session.GetValue(FeatureCatalog.Brightness));
    }

    [Fact]
    public async Task Reconnects_when_the_handle_goes_stale()
    {
        var session = await OpenAsync();
        _monitor.InvalidateHandles();

        var result = await session.WriteAsync(FeatureCatalog.Brightness, 55);

        Assert.Equal(FeatureStatus.Confirmed, result.Status);
        Assert.Equal(55u, _monitor.Current(Vcp.Brightness));
    }

    [Fact]
    public async Task Write_that_makes_the_monitor_reconnect_is_still_confirmed()
    {
        // Contrast isn't marked as a mode switch; the session must cope anyway when the monitor drops after a write.
        var session = await OpenAsync();
        _monitor.ReconnectAfterWrite(Vcp.Contrast);

        var result = await session.WriteAsync(FeatureCatalog.Contrast, 60);

        Assert.Equal(FeatureStatus.Confirmed, result.Status);
        Assert.Equal([(Vcp.Contrast, 60u)], _monitor.Writes);
    }

    [Fact]
    public async Task Mode_switch_waits_for_the_monitor_to_settle()
    {
        _monitor.SetRegister(Vcp.AsusHdrMode, 0x0102, 0x0207);
        var session = await OpenAsync();
        _monitor.ReportStaleValuesAfterWrite(Vcp.AsusHdrMode, reads: 3);

        var result = await session.WriteAsync(FeatureCatalog.HdrMode, 0x0104);

        Assert.Equal(FeatureStatus.Confirmed, result.Status);
        Assert.Equal(0x0104u, result.Value);
    }

    [Fact]
    public async Task Actions_are_not_read_back()
    {
        // Pixel cleaning: the monitor may stop answering, so the write is trusted rather than verified.
        var session = await OpenAsync();
        _monitor.IgnoreWrites(Vcp.AsusToggles2);

        var result = await session.WriteAsync(FeatureCatalog.PixelCleaning, 1);

        Assert.Equal(FeatureStatus.Confirmed, result.Status);
        Assert.Equal([(Vcp.AsusToggles2, 0x6839u)], _monitor.Writes);
    }

    [Fact]
    public async Task Choices_confirmed_on_the_monitor_are_sent_once_and_awaited()
    {
        // Proximity "Tailored": the monitor shows a prompt and keeps the old value until the user confirms it.
        var session = await OpenAsync();
        _monitor.IgnoreWrites(Vcp.AsusProximitySensor);
        var confirmed = NextConfirmedValue(session, FeatureCatalog.ProximityDistance);

        var result = await session.WriteAsync(FeatureCatalog.ProximityDistance, 0xFF);
        Assert.Equal(FeatureStatus.AwaitingConfirmation, result.Status);

        _monitor.SetRegister(Vcp.AsusProximitySensor, 0x05FF, 0x0FFF);

        Assert.Equal(new FeatureValue(FeatureCatalog.ProximityDistance, 0xFF, 0xFF, FeatureStatus.Confirmed), await confirmed);
        Assert.Equal([(Vcp.AsusProximitySensor, 0x05FFu)], _monitor.Writes);
    }

    [Fact]
    public async Task Unconfirmed_choices_revert_to_what_the_monitor_reports()
    {
        var session = await OpenAsync();
        _monitor.IgnoreWrites(Vcp.AsusProximitySensor);
        var confirmed = NextConfirmedValue(session, FeatureCatalog.ProximityDistance);

        await session.WriteAsync(FeatureCatalog.ProximityDistance, 0xFF);

        Assert.Equal(1u, (await confirmed).Value);
        Assert.Single(_monitor.Writes);
    }

    [Fact]
    public async Task Raw_reads_return_codes_outside_the_catalog()
    {
        _monitor.SetRegister(Vcp.AsusVcpVersion, 0xB1, 0x0217);
        var session = await OpenAsync();

        var reply = await session.ReadRawAsync(Vcp.AsusVcpVersion);

        Assert.Equal(0x0217u, reply?.Maximum);
    }

    [Fact]
    public async Task Read_only_features_are_rejected()
    {
        var session = await OpenAsync();

        await Assert.ThrowsAsync<NotSupportedException>(() => session.WriteAsync(FeatureCatalog.ProximitySensitivity, 3));
        Assert.Empty(_monitor.Writes);
    }

    [Fact]
    public async Task Unsupported_features_are_rejected()
    {
        var session = await OpenAsync();

        await Assert.ThrowsAsync<NotSupportedException>(() => session.WriteAsync(FeatureCatalog.BoundaryDetection, 1));
    }

    private static Task<FeatureValue> NextConfirmedValue(MonitorSession session, Feature feature)
    {
        var completion = new TaskCompletionSource<FeatureValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        session.ValueChanged += (_, e) =>
        {
            if (e.Value.Feature == feature && e.Value.Status == FeatureStatus.Confirmed)
            {
                completion.TrySetResult(e.Value);
            }
        };
        return completion.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    private async Task<MonitorSession> OpenAsync()
    {
        var connection = new MonitorConnection(Id, "PG32UCWM", _monitor.OpenChannel());
        _session = await MonitorSession.OpenAsync(connection, _monitor.Enumerator(Id), MonitorSessionOptions.Instant);
        return _session;
    }
}
