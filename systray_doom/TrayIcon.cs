using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

using static Crayon.Output;

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

    // public delegate LRESULT? ContextMenuHandler(HWND hwnd, int x, int y);
    // public ContextMenuHandler? ContextMenu;
    // public delegate LRESULT? MouseMoveHandler(HWND hwnd, int x, int y);
    // public MouseMoveHandler? MouseMove;
    public delegate LRESULT? CallbackMessageHandlerDelegate(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam);
    public CallbackMessageHandlerDelegate? CallbackMessageHandler;

    private readonly WindowSubclassHandler? _windowSubclassHandler = null;
    private static readonly uint s_taskbarCreatedWindowMessage = PInvoke.RegisterWindowMessage("TaskbarCreated");

    public TrayIcon(Guid guid, HWND ownerHwnd, bool shouldHandleMessages = true, uint? callbackMessage = null)
    {
        Guid = guid;
        OwnerHwnd = ownerHwnd;

        if (shouldHandleMessages)
        {
            _windowSubclassHandler = new WindowSubclassHandler(ownerHwnd, HandleMessage);

            // RegisterWindowMessage is global, so it's probably not the *best*
            // idea to take one of these slots, but opinions seem mixed.
            //
            // Against: https://stackoverflow.com/questions/4406909/using-wm-user-wm-app-or-registerwindowmessage
            // For: http://www.flounder.com/messages.htm
            // See also: https://stackoverflow.com/questions/30843497/wm-user-vs-wm-app
            callbackMessage ??= PInvoke.RegisterWindowMessage($"TrayIconMessage-{Guid}");
        }
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

    private LRESULT? HandleMessage(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            // https://stackoverflow.com/a/65642709/788168
            case var cb when cb == CallbackMessage:
                var result = CallbackMessageHandler?.Invoke(hwnd, msg, wParam, lParam);

                // Short-circuit the default window proc.
                return result ?? PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);

            case var tkwm when tkwm == s_taskbarCreatedWindowMessage:
                // Fired if Explorer crashes & restarts, or if the primary
                // display DPI changes.
                //
                // https://learn.microsoft.com/en-us/windows/win32/shell/taskbar#taskbar-creation-notification
                Console.WriteLine(Dim("Taskbar created message received."));
                // TODO: recreate icon.
                //
                // Notes: it's clear that the Shell_NotifyIcon itself cannot be
                // the source of truth, since it disappears if Explorer crashes
                // (and I don't believe you can read any data from it). So we
                // need a proxy if we want a reliable icon.

                // TODO: recreate icon.

                // Short-circuit the default window proc.
                return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
        }
        return null;
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