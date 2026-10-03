using System.Runtime.InteropServices;

namespace DisplayToolkit.Core.Native;

/// <summary>CCD (Connecting and Configuring Displays) API from user32.dll.</summary>
internal static unsafe partial class DisplayConfig
{
    private const string Dll = "user32.dll";

    internal const uint QdcOnlyActivePaths = 0x2;
    internal const int ErrorSuccess = 0;
    internal const int ErrorInsufficientBuffer = 122;

    internal enum DeviceInfoType : uint
    {
        GetSourceName = 1,
        GetTargetName = 2,
        GetAdvancedColorInfo = 9,
        SetAdvancedColorState = 10,
        SetHdrState = 16,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Luid
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PathSourceInfo
    {
        public Luid AdapterId;
        public uint Id;
        public uint ModeInfoIdx;
        public uint StatusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PathTargetInfo
    {
        public Luid AdapterId;
        public uint Id;
        public uint ModeInfoIdx;
        public uint OutputTechnology;
        public uint Rotation;
        public uint Scaling;
        public uint RefreshRateNumerator;
        public uint RefreshRateDenominator;
        public uint ScanLineOrdering;
        public int TargetAvailable;
        public uint StatusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PathInfo
    {
        public PathSourceInfo Source;
        public PathTargetInfo Target;
        public uint Flags;
    }

    /// <summary>Only the size matters: we never read mode info, but QueryDisplayConfig needs the buffer.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ModeInfo
    {
        public uint InfoType;
        public uint Id;
        public Luid AdapterId;
        public fixed byte Union[48];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DeviceInfoHeader
    {
        public DeviceInfoType Type;
        public uint Size;
        public Luid AdapterId;
        public uint Id;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SourceDeviceName
    {
        public DeviceInfoHeader Header;
        public fixed char ViewGdiDeviceName[32];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TargetDeviceName
    {
        public DeviceInfoHeader Header;
        public uint Flags;
        public uint OutputTechnology;
        public ushort EdidManufactureId;
        public ushort EdidProductCodeId;
        public uint ConnectorInstance;
        public fixed char MonitorFriendlyDeviceName[64];
        public fixed char MonitorDevicePath[128];

        public readonly string FriendlyName
        {
            get
            {
                fixed (char* name = MonitorFriendlyDeviceName)
                {
                    return new string(name);
                }
            }
        }

        public readonly string DevicePath
        {
            get
            {
                fixed (char* path = MonitorDevicePath)
                {
                    return new string(path);
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AdvancedColorInfo
    {
        public DeviceInfoHeader Header;
        public uint Value;
        public uint ColorEncoding;
        public uint BitsPerColorChannel;

        public readonly bool IsSupported => (Value & 0x1) != 0;
        public readonly bool IsEnabled => (Value & 0x2) != 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SetState
    {
        public DeviceInfoHeader Header;
        public uint Value;
    }

    [LibraryImport(Dll)]
    internal static partial int GetDisplayConfigBufferSizes(uint flags, out uint pathCount, out uint modeCount);

    [LibraryImport(Dll)]
    internal static partial int QueryDisplayConfig(
        uint flags, ref uint pathCount, PathInfo* paths, ref uint modeCount, ModeInfo* modes, nint topology);

    [LibraryImport(Dll)]
    internal static partial int DisplayConfigGetDeviceInfo(DeviceInfoHeader* packet);

    [LibraryImport(Dll)]
    internal static partial int DisplayConfigSetDeviceInfo(DeviceInfoHeader* packet);

    /// <summary>Returns the active display paths, retrying if the topology changes between the two calls.</summary>
    internal static PathInfo[] GetActivePaths()
    {
        while (true)
        {
            if (GetDisplayConfigBufferSizes(QdcOnlyActivePaths, out var pathCount, out var modeCount) != ErrorSuccess)
            {
                return [];
            }

            var paths = new PathInfo[pathCount];
            var modes = new ModeInfo[modeCount];
            int result;
            fixed (PathInfo* pathsPtr = paths)
            fixed (ModeInfo* modesPtr = modes)
            {
                result = QueryDisplayConfig(QdcOnlyActivePaths, ref pathCount, pathsPtr, ref modeCount, modesPtr, 0);
            }

            if (result == ErrorInsufficientBuffer)
            {
                continue;
            }
            return result == ErrorSuccess ? paths[..(int)pathCount] : [];
        }
    }

    internal static string? GetSourceGdiName(in PathInfo path)
    {
        var request = new SourceDeviceName
        {
            Header = Header(DeviceInfoType.GetSourceName, (uint)sizeof(SourceDeviceName), path.Source.AdapterId, path.Source.Id),
        };
        return DisplayConfigGetDeviceInfo(&request.Header) == ErrorSuccess ? new string(request.ViewGdiDeviceName) : null;
    }

    internal static TargetDeviceName? GetTargetName(in PathInfo path)
    {
        var request = new TargetDeviceName
        {
            Header = Header(DeviceInfoType.GetTargetName, (uint)sizeof(TargetDeviceName), path.Target.AdapterId, path.Target.Id),
        };
        return DisplayConfigGetDeviceInfo(&request.Header) == ErrorSuccess ? request : null;
    }

    internal static AdvancedColorInfo? GetAdvancedColorInfo(in PathInfo path)
    {
        var request = new AdvancedColorInfo
        {
            Header = Header(DeviceInfoType.GetAdvancedColorInfo, (uint)sizeof(AdvancedColorInfo), path.Target.AdapterId, path.Target.Id),
        };
        return DisplayConfigGetDeviceInfo(&request.Header) == ErrorSuccess ? request : null;
    }

    internal static bool SetHdr(in PathInfo path, bool enable, bool useHdrStateApi)
    {
        var type = useHdrStateApi ? DeviceInfoType.SetHdrState : DeviceInfoType.SetAdvancedColorState;
        var request = new SetState
        {
            Header = Header(type, (uint)sizeof(SetState), path.Target.AdapterId, path.Target.Id),
            Value = enable ? 1u : 0u,
        };
        return DisplayConfigSetDeviceInfo(&request.Header) == ErrorSuccess;
    }

    private static DeviceInfoHeader Header(DeviceInfoType type, uint size, Luid adapterId, uint id) =>
        new() { Type = type, Size = size, AdapterId = adapterId, Id = id };
}
