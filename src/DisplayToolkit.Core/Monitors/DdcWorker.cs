namespace DisplayToolkit.Core.Monitors;

/// <summary>
/// Runs DDC/CI work for one monitor on a dedicated thread, one item at a time. DDC calls block for tens of
/// milliseconds, so they must never run on the UI thread, and the bus can't handle concurrent calls.
/// </summary>
/// <remarks>
/// Items enqueued with the same coalescing key replace a queued (not yet started) item: the newest work runs once and
/// every caller gets its result. This keeps a dragged slider from queueing dozens of stale writes.
/// </remarks>
internal sealed class DdcWorker : IDisposable
{
    private readonly Lock _gate = new();
    private readonly LinkedList<WorkItem> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly Thread _thread;
    private bool _disposed;

    public DdcWorker(string name)
    {
        _thread = new Thread(Run) { IsBackground = true, Name = $"DDC {name}" };
        _thread.Start();
    }

    public async Task<T> Enqueue<T>(Func<T> work, string? coalescingKey = null)
    {
        var waiter = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var queued = coalescingKey is null ? null : _queue.FirstOrDefault(item => item.CoalescingKey == coalescingKey);
            if (queued is not null)
            {
                queued.Work = () => work();
                queued.Waiters.Add(waiter);
            }
            else
            {
                _queue.AddLast(new WorkItem(coalescingKey, () => work(), waiter));
                _signal.Release();
            }
        }
        return (T)(await waiter.Task)!;
    }

    public void Dispose()
    {
        List<WorkItem> abandoned;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            abandoned = [.. _queue];
            _queue.Clear();
        }

        _signal.Release();
        _thread.Join(TimeSpan.FromSeconds(5));
        _signal.Dispose();

        foreach (var waiter in abandoned.SelectMany(item => item.Waiters))
        {
            waiter.TrySetException(new ObjectDisposedException(nameof(DdcWorker)));
        }
    }

    private void Run()
    {
        while (true)
        {
            _signal.Wait();
            WorkItem item;
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }
                if (_queue.First is null)
                {
                    continue;
                }
                item = _queue.First.Value;
                _queue.RemoveFirst();
            }

            try
            {
                var result = item.Work();
                item.Waiters.ForEach(waiter => waiter.TrySetResult(result));
            }
            catch (Exception exception)
            {
                item.Waiters.ForEach(waiter => waiter.TrySetException(exception));
            }
        }
    }

    private sealed class WorkItem(string? coalescingKey, Func<object?> work, TaskCompletionSource<object?> waiter)
    {
        public string? CoalescingKey { get; } = coalescingKey;

        public Func<object?> Work { get; set; } = work;

        public List<TaskCompletionSource<object?>> Waiters { get; } = [waiter];
    }
}
