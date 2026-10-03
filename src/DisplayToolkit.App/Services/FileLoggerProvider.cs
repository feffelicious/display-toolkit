using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;

namespace DisplayToolkit.App.Services;

/// <summary>
/// Appends log lines to <see cref="AppPaths.LogFile"/>, starting over when the file passes 1 MB. One plain-text file
/// that users can attach to bug reports.
/// </summary>
internal sealed class FileLoggerProvider : ILoggerProvider
{
    private const long MaxFileSize = 1024 * 1024;

    private readonly Lock _gate = new();
    private readonly StreamWriter _writer;

    public FileLoggerProvider()
    {
        var file = new FileInfo(AppPaths.LogFile);
        var mode = file.Exists && file.Length > MaxFileSize ? FileMode.Create : FileMode.Append;
        _writer = new StreamWriter(new FileStream(file.FullName, mode, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true };
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName[(categoryName.LastIndexOf('.') + 1)..]);

    public void Dispose() => _writer.Dispose();

    private void Write(LogLevel level, string category, string message, Exception? exception)
    {
        var line = string.Create(CultureInfo.InvariantCulture, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {Abbreviate(level)} {category}: {message}");
        lock (_gate)
        {
            _writer.WriteLine(line);
            if (exception is not null)
            {
                _writer.WriteLine(exception);
            }
        }
    }

    private static string Abbreviate(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        _ => "CRT",
    };

    private sealed class FileLogger(FileLoggerProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                provider.Write(logLevel, category, formatter(state, exception), exception);
            }
        }
    }
}
