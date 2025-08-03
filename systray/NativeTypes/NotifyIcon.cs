using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Systray.NativeTypes;

internal struct NOTIFYICONDATAW
{
    internal uint cbSize;
    internal HWND hWnd;
    internal uint uID;
    internal NOTIFY_ICON_DATA_FLAGS uFlags;
    internal uint uCallbackMessage;
    internal HICON hIcon;
    internal __char_128 szTip;
    internal NOTIFY_ICON_STATE dwState;
    internal NOTIFY_ICON_STATE dwStateMask;
    internal __char_256 szInfo;
    internal _Anonymous_e__Union Anonymous;
    internal __char_64 szInfoTitle;
    internal NOTIFY_ICON_INFOTIP_FLAGS dwInfoFlags;
    internal Guid guidItem;
    internal HICON hBalloonIcon;

    [StructLayout(LayoutKind.Explicit)]
    internal partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        internal uint uTimeout;

        [FieldOffset(0)]
        internal uint uVersion;
    }
};

internal static class NotifyIcon
{
    [DllImport("SHELL32.dll", ExactSpelling = true, EntryPoint = "Shell_NotifyIconW")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("windows5.1.2600")]
    internal static extern unsafe BOOL Shell_NotifyIcon(NOTIFY_ICON_MESSAGE dwMessage, NOTIFYICONDATAW* lpData);

    internal static unsafe BOOL Shell_NotifyIcon(NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData)
    {
        fixed (NOTIFYICONDATAW* lpDataLocal = &lpData)
        {
            BOOL __result = Shell_NotifyIcon(dwMessage, lpDataLocal);
            return __result;
        }
    }
}
