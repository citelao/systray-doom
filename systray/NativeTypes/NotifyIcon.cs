using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Systray.NativeTypes;

// The CsWin32-generated Shell_NotifyIcon APIs are platform-specific (and don't
// support the AnyCpu build), even though in practice they do not change
// per-architecture. So match WinForms & simply define the struct here.
//
// Adapted from CsWin32 definition & WinForms version.
//
// https://github.com/dotnet/winforms/blob/main/src/System.Windows.Forms.Primitives/src/Windows/Win32/UI/Shell/NOTIFYICONDATAW.cs
//
// See https://github.com/microsoft/CsWin32/discussions/592
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct NOTIFYICONDATAW
{
    public uint cbSize;
    public HWND hWnd;
    public uint uID;
    public NOTIFY_ICON_DATA_FLAGS uFlags;
    public uint uCallbackMessage;
    public HICON hIcon;
    public fixed char _szTip[128];
    public NOTIFY_ICON_STATE dwState;
    public NOTIFY_ICON_STATE dwStateMask;
    public fixed char _szInfo[256];
    public _Anonymous_e__Union Anonymous;
    public fixed char _szInfoTitle[64];
    public NOTIFY_ICON_INFOTIP_FLAGS dwInfoFlags;
    public Guid guidItem;
    public HICON hBalloonIcon;

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        public uint uTimeout;

        [FieldOffset(0)]
        public uint uVersion;
    }

    private Span<char> szTip
    {
        get { fixed (char* c = _szTip) { return new Span<char>(c, 128); } }
    }

    public ReadOnlySpan<char> Tip
    {
        get => szTip.SliceAtFirstNull();
        set => value.CopyAndTerminate(szTip);
    }

    private Span<char> szInfo
    {
        get { fixed (char* c = _szInfo) { return new Span<char>(c, 256); } }
    }

    public ReadOnlySpan<char> Info
    {
        get => szInfo.SliceAtFirstNull();
        set => value.CopyAndTerminate(szInfo);
    }

    private Span<char> szInfoTitle
    {
        get { fixed (char* c = _szInfoTitle) { return new Span<char>(c, 64); } }
    }

    public ReadOnlySpan<char> InfoTitle
    {
        get => szInfoTitle.SliceAtFirstNull();
        set => value.CopyAndTerminate(szInfoTitle);
    }
};

internal static partial class NotifyIcon
{
    [LibraryImport("SHELL32.dll", EntryPoint = "Shell_NotifyIconW")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("windows5.1.2600")]
    internal static unsafe partial int Shell_NotifyIcon(uint dwMessage, NOTIFYICONDATAW* lpData);

    internal static unsafe BOOL Shell_NotifyIcon(NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData)
    {
        fixed (NOTIFYICONDATAW* lpDataLocal = &lpData)
        {
            BOOL __result = (BOOL)Shell_NotifyIcon((uint)dwMessage, lpDataLocal);
            return __result;
        }
    }
}
