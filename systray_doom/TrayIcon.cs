using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

internal class TrayIcon
{
    private string _tooltip = string.Empty;
    public string Tooltip {
        set { SetTooltip(value); }
        get { return _tooltip; }
    }

    public readonly Guid Guid;
    public readonly HWND OwnerHwnd;

    public TrayIcon(Guid guid, HWND ownerHwnd)
    {
        Guid = guid;
        OwnerHwnd = ownerHwnd;

        // https://github.com/microsoft/WindowsAppSDK/discussions/519
        // https://github.com/microsoft/WindowsAppSDK/issues/713
        // https://github.com/File-New-Project/EarTrumpet/blob/bd42f1e235386c35c0989df8f2af8aba951f1848/EarTrumpet/UI/Helpers/ShellNotifyIcon.cs#L18
        // https://learn.microsoft.com/en-us/windows/win32/shell/notification-area
        // https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-notifyicondataa
        // https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyicona
        var notificationIconData = new NOTIFYICONDATAW
        {
            // Required. You need to include the size of this struct.
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),

            // Required. An HWND is required to register the icon with the system.
            // Window messages go there.
            hWnd = ownerHwnd,

            // Required. Indicates which of the other members contain valid data.
            // NIF_TIP and NIF_SHOWTIP are only required if you want to use szTip.
            uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_GUID | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP,

            // TODO
            uCallbackMessage = 0,

            // Required. The icon to display.
            // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-loadicona
            // https://learn.microsoft.com/en-us/windows/win32/menurc/about-icons
            hIcon = PInvoke.LoadIcon(HINSTANCE.Null, PInvoke.IDI_APPLICATION),

            // Optional. You probably want a tooltip for your icon, though.
            szTip = _tooltip,

            // Required. A GUID to identify the icon. This should be persistent across
            // launches and unique to your app!
            guidItem = guid,

            Anonymous = new()
            {
                // Recommended. VERSION_4 has been present since Vista and gives your
                // app much richer window messages & more control over the icon tooltip.
                //
                // https://stackoverflow.com/q/41649303/788168
                uVersion = PInvoke.NOTIFYICON_VERSION_4
            }
        };
        if (!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, notificationIconData))
        {
            throw new Exception("Failed to add icon to the notification area.");
        }
        if(!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, notificationIconData))
        {
            throw new Exception("Failed to set version of icon in the notification area.");
        }
    }

    private void SetTooltip(string newTip)
    {
        var notificationIconData = new NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
            hWnd = OwnerHwnd,
            uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP | NOTIFY_ICON_DATA_FLAGS.NIF_GUID,
            szTip = newTip,
            guidItem = Guid,
        };
        if (!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, notificationIconData))
        {
            throw new Exception("Failed to modify icon in the notification area.");
        }
        _tooltip = newTip;
    }
}