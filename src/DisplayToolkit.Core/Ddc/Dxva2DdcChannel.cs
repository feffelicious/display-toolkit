using System.Runtime.InteropServices;
using System.Text;
using DisplayToolkit.Core.Native;

namespace DisplayToolkit.Core.Ddc;

/// <summary>DDC/CI over the Windows Monitor Configuration API. Owns one physical-monitor handle.</summary>
internal sealed class Dxva2DdcChannel(nint physicalMonitor) : IDdcChannel
{
    private nint _handle = physicalMonitor;

    ~Dxva2DdcChannel() => Release();

    public string GetCapabilities()
    {
        ThrowIfDisposed();
        if (!Dxva2.GetCapabilitiesStringLength(_handle, out var length))
        {
            throw Failure(nameof(Dxva2.GetCapabilitiesStringLength));
        }

        var buffer = new byte[length];
        unsafe
        {
            fixed (byte* bufferPtr = buffer)
            {
                if (!Dxva2.CapabilitiesRequestAndCapabilitiesReply(_handle, bufferPtr, length))
                {
                    throw Failure(nameof(Dxva2.CapabilitiesRequestAndCapabilitiesReply));
                }
            }
        }

        var terminator = Array.IndexOf(buffer, (byte)0);
        return Encoding.ASCII.GetString(buffer, 0, terminator >= 0 ? terminator : buffer.Length);
    }

    public VcpReply Get(byte code)
    {
        ThrowIfDisposed();
        return Dxva2.GetVCPFeatureAndVCPFeatureReply(_handle, code, out _, out var current, out var maximum)
            ? new VcpReply(current, maximum)
            : throw Failure($"Read 0x{code:X2}");
    }

    public void Set(byte code, uint value)
    {
        ThrowIfDisposed();
        if (!Dxva2.SetVCPFeature(_handle, code, value))
        {
            throw Failure($"Write 0x{code:X2}");
        }
    }

    public void Dispose()
    {
        Release();
        GC.SuppressFinalize(this);
    }

    private void Release()
    {
        var handle = Interlocked.Exchange(ref _handle, -1);
        if (handle != -1)
        {
            Dxva2.DestroyPhysicalMonitor(handle);
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_handle == -1, this);

    private static DdcException Failure(string operation) => new(operation, Marshal.GetLastPInvokeError());
}
