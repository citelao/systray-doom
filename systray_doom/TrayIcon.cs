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

    // Fired if Explorer crashes & restarts, or if the primary display DPI changes.
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

        Create();
    }

    // Re-create the icon; useful if Explorer crashes (though we handle that
    // automatically).
    public void Create()
    {
        var notificationIconData = new TrayIconMessageBuilder(guid: Guid)
        {
            HWND = OwnerHwnd,
            Tooltip = _tooltip,
            Icon = _icon,
            CallbackMessage = CallbackMessage,
        }.Build();
        // PInvokeHelpers.THROW_IF_FALSE(PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, notificationIconData), "Failed to add icon to the notification area.");
        PInvokeHelpers.THROW_IF_FALSE(PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, notificationIconData), "Failed to add icon to the notification area.");
        PInvokeHelpers.THROW_IF_FALSE(PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, notificationIconData), "Failed to set version of icon in the notification area.");
    }

    // public bool TryCreate()
    // {
    //     try
    //     {
    //         Create();
    //         return true;
    //     }
    //     catch (Exception ex)
    //     {
    //         // Console.Error.WriteLine($"Failed to create tray icon: {ex.Message}");
    //         return false;
    //     }
    // }

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

                // Re-create the icon.
                var notificationIconData = new TrayIconMessageBuilder(guid: Guid).Build();
                // PInvokeHelpers.THROW_IF_FALSE(PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, notificationIconData), "Failed to add icon to the notification area.");
                PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, notificationIconData);
                Create();

                // Short-circuit the default window proc.
                return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
        }
        return null;
    }

    private void SetTooltip(string newTip)
    {
        // if (!TryCreate())
        // {
        //     return;
        // }

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

    // TODO: hangs if called when explorer has crashed (or the icon has been removed)
    private void SetIcon(HICON newIcon)
    {
        // if (!TryCreate())
        // {
        //     return;
        // }

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