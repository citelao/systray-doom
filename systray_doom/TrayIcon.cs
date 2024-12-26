using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

internal class TrayIcon
{
    private string _tooltip = string.Empty;
    public string Tooltip {
        set { SetTooltip(value); }
        get { return _tooltip; }
    }

    private HICON _icon = PInvoke.LoadIcon(HINSTANCE.Null, PInvoke.IDI_APPLICATION);
    public HICON Icon {
        set { SetIcon(value); }
        get { return _icon; }
    }

    public readonly Guid Guid;
    public readonly HWND OwnerHwnd;
    public readonly uint? CallbackMessage = null;

    public TrayIcon(Guid guid, HWND ownerHwnd, uint? callbackMessage = null)
    {
        Guid = guid;
        OwnerHwnd = ownerHwnd;
        CallbackMessage = callbackMessage;

        var notificationIconData = new TrayIconMessageBuilder(guid: Guid)
        {
            HWND = ownerHwnd,
            Tooltip = _tooltip,
            Icon = _icon,
            CallbackMessage = CallbackMessage,
        }.Build();
        if (!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, notificationIconData))
        {
            throw new Exception("Failed to add icon to the notification area.");
        }
        if(!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, notificationIconData))
        {
            throw new Exception("Failed to set version of icon in the notification area.");
        }
    }

    public void Focus()
    {
        // TODO: doesn't work?
        PInvokeHelpers.THROW_IF_FALSE(PInvoke.Shell_NotifyIcon(
            NOTIFY_ICON_MESSAGE.NIM_SETFOCUS,
            new TrayIconMessageBuilder(guid: Guid).Build()));
    }

    private void SetTooltip(string newTip)
    {
        var notificationIconData = new TrayIconMessageBuilder(guid: Guid)
        {
            Tooltip = newTip,
        }.Build();
        if (!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, notificationIconData))
        {
            throw new Exception("Failed to modify icon in the notification area.");
        }
        _tooltip = newTip;
    }

    private void SetIcon(HICON newIcon)
    {
        var notificationIconData = new TrayIconMessageBuilder(guid: Guid)
        {
            Icon = newIcon,
        }.Build();
        if (!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, notificationIconData))
        {
            throw new Exception("Failed to modify icon in the notification area.");
        }
        _icon = newIcon;
    }
}