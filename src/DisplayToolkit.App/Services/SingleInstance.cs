namespace DisplayToolkit.App.Services;

/// <summary>
/// Keeps one copy of the app running. A second launch signals the first (which opens its flyout) and exits.
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\DisplayToolkit.SingleInstance";
    private const string ActivateEventName = @"Local\DisplayToolkit.Activate";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activate;
    private RegisteredWaitHandle? _registration;

    public SingleInstance()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        IsFirst = createdNew;
        _activate = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);
    }

    public bool IsFirst { get; }

    /// <summary>Tells the running instance to show itself.</summary>
    public void SignalFirstInstance() => _activate.Set();

    /// <summary>Calls <paramref name="onActivated"/> (on a thread-pool thread) whenever another launch signals.</summary>
    public void ListenForActivation(Action onActivated) =>
        _registration = ThreadPool.RegisterWaitForSingleObject(_activate, (_, _) => onActivated(), null, Timeout.Infinite, executeOnlyOnce: false);

    public void Dispose()
    {
        _registration?.Unregister(null);
        _activate.Dispose();
        if (IsFirst)
        {
            _mutex.ReleaseMutex();
        }
        _mutex.Dispose();
    }
}
