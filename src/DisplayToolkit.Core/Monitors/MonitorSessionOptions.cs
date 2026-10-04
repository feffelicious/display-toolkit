namespace DisplayToolkit.Core.Monitors;

/// <summary>Timing for DDC/CI traffic. The defaults come from measurements on a PG32UCWM.</summary>
public sealed record MonitorSessionOptions
{
    public static MonitorSessionOptions Default { get; } = new();

    /// <summary>Minimum quiet time between two DDC/CI calls. Monitors drop commands that arrive too fast.</summary>
    public TimeSpan CommandSpacing { get; init; } = TimeSpan.FromMilliseconds(20);

    /// <summary>Delay before reading back a write to confirm it.</summary>
    public TimeSpan VerifyDelay { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>How many times a failed call or unconfirmed write is retried.</summary>
    public int Retries { get; init; } = 2;

    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMilliseconds(150);

    /// <summary>
    /// How long a mode-switching write (HDR preset) may take to read back correctly. True Black 400 needed several
    /// seconds, during which the monitor reported stale values and its handle became invalid.
    /// </summary>
    public TimeSpan ModeSwitchTimeout { get; init; } = TimeSpan.FromSeconds(12);

    public TimeSpan ModeSwitchPollInterval { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>How long to wait for the user to confirm a choice on the monitor (and for the monitor to apply it).</summary>
    public TimeSpan ConfirmationTimeout { get; init; } = TimeSpan.FromMinutes(2);

    public TimeSpan ConfirmationPollInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>How long the monitor takes to apply a picture mode reset before its settings are read again.</summary>
    public TimeSpan ResetSettling { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>Attempts for the capabilities request, which is slower and more fragile than single reads.</summary>
    public int CapabilitiesAttempts { get; init; } = 5;

    public TimeSpan CapabilitiesRetryDelay { get; init; } = TimeSpan.FromMilliseconds(750);

    /// <summary>Instant timing for tests against a simulated monitor.</summary>
    public static MonitorSessionOptions Instant { get; } = new()
    {
        CommandSpacing = TimeSpan.Zero,
        VerifyDelay = TimeSpan.Zero,
        RetryDelay = TimeSpan.Zero,
        ModeSwitchTimeout = TimeSpan.FromMilliseconds(200),
        ModeSwitchPollInterval = TimeSpan.Zero,
        ConfirmationTimeout = TimeSpan.FromMilliseconds(200),
        ConfirmationPollInterval = TimeSpan.FromMilliseconds(10),
        CapabilitiesRetryDelay = TimeSpan.Zero,
        ResetSettling = TimeSpan.Zero,
    };
}
