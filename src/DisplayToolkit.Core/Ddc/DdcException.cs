namespace DisplayToolkit.Core.Ddc;

/// <summary>A DDC/CI call failed: the monitor didn't answer, or the handle is no longer valid.</summary>
public sealed class DdcException : Exception
{
    public DdcException()
    {
    }

    public DdcException(string message)
        : base(message)
    {
    }

    public DdcException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public DdcException(string operation, int win32Error)
        : base($"{operation} failed (Win32 error 0x{win32Error:X8}).")
    {
        Win32Error = win32Error;
    }

    public int Win32Error { get; }
}
