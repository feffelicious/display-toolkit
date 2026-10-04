using System.Runtime.InteropServices;

namespace DisplayToolkit.App.Native;

internal static unsafe partial class Shell32
{
    internal const uint NimAdd = 0x0;
    internal const uint NimModify = 0x1;
    internal const uint NimDelete = 0x2;
    internal const uint NimSetVersion = 0x4;

    internal const uint NifMessage = 0x1;
    internal const uint NifIcon = 0x2;
    internal const uint NifTip = 0x4;
    internal const uint NifInfo = 0x10;
    internal const uint NifShowTip = 0x80;

    internal const uint NotifyIconVersion4 = 4;

    // Notifications delivered in LOWORD(lParam) with NOTIFYICON_VERSION_4.
    internal const int NinSelect = 0x400;
    internal const int NinKeySelect = 0x401;
    internal const int NinBalloonUserClick = 0x405;
    internal const int WmContextMenu = 0x007B;

    [StructLayout(LayoutKind.Sequential)]
    internal struct NotifyIconData
    {
        public uint Size;
        public nint Window;
        public uint Id;
        public uint Flags;
        public uint CallbackMessage;
        public nint Icon;
        public fixed char Tip[128];
        public uint State;
        public uint StateMask;
        public fixed char Info[256];
        public uint TimeoutOrVersion;
        public fixed char InfoTitle[64];
        public uint InfoFlags;
        public Guid Item;
        public nint BalloonIcon;

        public void SetTip(string text)
        {
            fixed (char* tip = Tip)
            {
                Copy(text, new Span<char>(tip, 128));
            }
        }

        /// <summary>A notification (shown as a Windows toast) with a title and a line of text.</summary>
        public void SetInfo(string title, string text)
        {
            fixed (char* info = Info)
            {
                Copy(text, new Span<char>(info, 256));
            }
            fixed (char* infoTitle = InfoTitle)
            {
                Copy(title, new Span<char>(infoTitle, 64));
            }
        }

        private static void Copy(string text, Span<char> destination)
        {
            destination.Clear();
            text.AsSpan(0, Math.Min(text.Length, destination.Length - 1)).CopyTo(destination);
        }
    }

    [LibraryImport("shell32.dll", EntryPoint = "Shell_NotifyIconW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShellNotifyIcon(uint message, NotifyIconData* data);
}
