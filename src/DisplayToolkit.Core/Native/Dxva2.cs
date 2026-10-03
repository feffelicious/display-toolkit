using System.Runtime.InteropServices;

namespace DisplayToolkit.Core.Native;

/// <summary>Monitor Configuration API (DDC/CI) from dxva2.dll.</summary>
internal static unsafe partial class Dxva2
{
    private const string Dll = "dxva2.dll";

    [StructLayout(LayoutKind.Sequential)]
    internal struct PhysicalMonitor
    {
        public nint Handle;
        public fixed char Description[128];
    }

    [LibraryImport(Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetNumberOfPhysicalMonitorsFromHMONITOR(nint hMonitor, out uint count);

    [LibraryImport(Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetPhysicalMonitorsFromHMONITOR(nint hMonitor, uint arraySize, PhysicalMonitor* monitors);

    [LibraryImport(Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyPhysicalMonitor(nint hPhysicalMonitor);

    [LibraryImport(Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetCapabilitiesStringLength(nint hPhysicalMonitor, out uint length);

    [LibraryImport(Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CapabilitiesRequestAndCapabilitiesReply(nint hPhysicalMonitor, byte* buffer, uint length);

    [LibraryImport(Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetVCPFeatureAndVCPFeatureReply(
        nint hPhysicalMonitor, byte vcpCode, out uint codeType, out uint currentValue, out uint maximumValue);

    [LibraryImport(Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetVCPFeature(nint hPhysicalMonitor, byte vcpCode, uint newValue);
}
