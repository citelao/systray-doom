namespace Systray;

using System.Runtime.InteropServices;
using Systray.NativeTypes;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

using static Crayon.Output;

// TODO: better name in Taskbar Personalization menu
public class TrayIcon
{
    private string _tooltip = string.Empty;

    // Tooltip to display when hovering the icon, also used as its accessible
    // name. Will be truncated to 128 characters (including the terminating
    // null).
    public string Tooltip
    {
        set { SetTooltip(value); }
        get { return _tooltip; }
    }

    private NoReleaseHicon _icon = new(PInvokeSystray.LoadIcon(HINSTANCE.Null, PInvokeSystray.IDI_APPLICATION));
    public NoReleaseHicon Icon
    {
        set { SetIcon(value); }
        get { return _icon; }
    }

    public readonly Guid Guid;
    public readonly NoReleaseHwnd OwnerHwnd;
    public readonly uint? CallbackMessage = null;

    // Return true to indicate that the message was handled.
    public delegate bool ContextMenuHandler(NoReleaseHwnd hwnd, int x, int y);
    public ContextMenuHandler? ContextMenu;
    public delegate bool SelectHandler(NoReleaseHwnd hwnd, int x, int y);
    public SelectHandler? Select;
    // public delegate LRESULT? MouseMoveHandler(HWND hwnd, int x, int y);
    // public MouseMoveHandler? MouseMove;
    // public delegate LRESULT? CallbackMessageHandlerDelegate(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam);
    // public CallbackMessageHandlerDelegate? CallbackMessageHandler;

    // Hold a reference to the WindowSubclassHandler so it doesn't get GC'd; it
    // owns the window proc delegate.
    private readonly WindowSubclassHandler? _windowSubclassHandler = null;

    // Fired if Explorer crashes & restarts, or if the primary display DPI changes.
    private static readonly uint s_taskbarCreatedWindowMessage = PInvokeSystray.RegisterWindowMessage("TaskbarCreated");

    public TrayIcon(Guid guid, NoReleaseHwnd ownerHwnd, bool shouldHandleMessages = true, uint? callbackMessage = null)
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
            callbackMessage ??= PInvokeSystray.RegisterWindowMessage($"TrayIconMessage-{Guid}");
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
        PInvokeHelpers.THROW_IF_FALSE(PInvokeSystray.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, notificationIconData), "Failed to add icon to the notification area.");
        PInvokeHelpers.THROW_IF_FALSE(PInvokeSystray.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, notificationIconData), "Failed to set version of icon in the notification area.");
    }

    public void Focus()
    {
        // TODO: doesn't work?
        PInvokeHelpers.THROW_IF_FALSE(PInvokeSystray.Shell_NotifyIcon(
            NOTIFY_ICON_MESSAGE.NIM_SETFOCUS,
            new TrayIconMessageBuilder(guid: Guid).Build()));
    }

    private LRESULT? HandleMessage(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            // This is the special WM we registered, fired when something
            // happens in the systray.
            //
            // https://stackoverflow.com/a/65642709/788168
            case var cb when cb == CallbackMessage:
                var result = HandleCallbackMessage(hwnd, msg, wParam, lParam);

                // Short-circuit the default window proc.
                if (result != null)
                {
                    return result;
                }

                return new(PInvokeSystray.DefWindowProc(hwnd, msg, wParam, lParam));

            case var tkwm when tkwm == s_taskbarCreatedWindowMessage:
                // Fired if Explorer crashes & restarts, or if the primary
                // display DPI changes.
                //
                // https://learn.microsoft.com/en-us/windows/win32/shell/taskbar#taskbar-creation-notification
                Console.WriteLine(Dim("Taskbar created message received."));

                // Re-create the icon.
                var notificationIconData = new TrayIconMessageBuilder(guid: Guid).Build();
                // PInvokeHelpers.THROW_IF_FALSE(PInvokeSystray.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, notificationIconData), "Failed to add icon to the notification area.");
                PInvokeSystray.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, notificationIconData);
                Create();

                // Don't replace the default window proc.
                return new(PInvokeSystray.DefWindowProc(hwnd, msg, wParam, lParam));
        }
        return null;
    }

    // Parse & dispatch well-known messages.
    private LRESULT? HandleCallbackMessage(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        // Console.WriteLine("Tray icon message received.");
        var ev = (uint)PInvokeHelpers.LOWORD(lParam.Value);
        var iconId = (uint)PInvokeHelpers.HIWORD(lParam.Value);

        // TODO: what coordinate system is this? I think it's global, but
        // affected by the DPI setting.
        //
        // A few months later: probably screen coordinates (the "official" term
        // for global coordinates), which are scaled on non-DPI-aware apps. If
        // your app *is* per-monitor aware, then these are legit, physical
        // pixels.
        var x = PInvokeHelpers.GET_X_LPARAM(wParam.Value);
        var y = PInvokeHelpers.GET_Y_LPARAM(wParam.Value);

        // https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyicona#remarks
        // https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-notifyicondataa#:~:text=but%20the%20interpretation%20of%20the%20lParam%20and%20wParam%20parameters%20of%20that%20message%20is%20changed%20as%20follows%3A
        switch (ev)
        {
            case PInvokeSystray.WM_CONTEXTMENU:
                // var pt = new Point(x, y);
                // var client = PInvokeSystray.ScreenToClient(hwnd, ref pt);
                // Console.WriteLine($"Client: {pt.X}, {pt.Y}");
                return (ContextMenu?.Invoke(new(hwnd), x, y) ?? false) ? new LRESULT(0) : null;

            case PInvokeSystray.WM_MOUSEMOVE:
                Console.WriteLine(Dim($"Tray icon mouse move for {iconId} ({x}, {y})."));
                break;

            case PInvokeSystray.WM_LBUTTONDOWN:
                Console.WriteLine(Dim($"Tray icon left button down for {iconId} ({x}, {y})."));
                break;

            case PInvokeSystray.WM_LBUTTONUP:
                Console.WriteLine(Dim($"Tray icon left button up for {iconId} ({x}, {y})."));
                break;

            case PInvokeSystray.WM_LBUTTONDBLCLK:
                Console.WriteLine(Dim($"Tray icon left button double click for {iconId} ({x}, {y})."));
                break;

            case PInvokeSystray.WM_RBUTTONDOWN:
                Console.WriteLine(Dim($"Tray icon right button down for {iconId} ({x}, {y})."));
                break;

            case PInvokeSystray.WM_RBUTTONUP:
                Console.WriteLine(Dim($"Tray icon right button up for {iconId} ({x}, {y})."));
                break;

            case PInvokeSystray.WM_MBUTTONDOWN:
                Console.WriteLine(Dim($"Tray icon middle button down for {iconId} ({x}, {y})."));
                break;

            case PInvokeSystray.WM_MBUTTONUP:
                Console.WriteLine(Dim($"Tray icon middle button up for {iconId} ({x}, {y})."));
                break;

            case PInvokeSystray.NIN_SELECT:
                Console.WriteLine(Dim($"Tray icon select for {iconId} ({x}, {y})."));
                return (Select?.Invoke(new(hwnd), x, y) ?? false) ? new LRESULT(0) : null;

            case PInvokeSystray.NIN_BALLOONSHOW:
                Console.WriteLine(Dim($"Tray icon balloon show for {iconId} ({x}, {y})."));
                break;

            case PInvokeSystray.NIN_BALLOONHIDE:
                Console.WriteLine(Dim($"Tray icon balloon hide for {iconId} ({x}, {y})."));
                break;

            case PInvokeSystray.NIN_BALLOONTIMEOUT:
                Console.WriteLine(Dim($"Tray icon balloon timeout for {iconId} ({x}, {y})."));
                break;

            case PInvokeSystray.NIN_BALLOONUSERCLICK:
                Console.WriteLine(Dim($"Tray icon balloon user click for {iconId} ({x}, {y})."));
                break;

            case PInvokeSystray.NIN_POPUPOPEN:
                Console.WriteLine(Dim($"Tray icon popup open for {iconId} ({x}, {y})."));
                break;

            case PInvokeSystray.NIN_POPUPCLOSE:
                Console.WriteLine(Dim($"Tray icon popup close for {iconId} ({x}, {y})."));
                break;

            default:
                Console.WriteLine(Dim($"Tray icon message: {ev}"));
                break;
        }

        return null;
    }

    private void SetTooltip(string newTip)
    {
        var notificationIconData = new TrayIconMessageBuilder(guid: Guid)
        {
            Tooltip = newTip,
        }.Build();
        if (!PInvokeSystray.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, notificationIconData))
        {
            throw new Exception("Failed to modify icon in the notification area.");
        }
        _tooltip = newTip;
    }

    private void SetIcon(NoReleaseHicon newIcon)
    {
        var notificationIconData = new TrayIconMessageBuilder(guid: Guid)
        {
            Icon = newIcon,
        }.Build();
        if (!PInvokeSystray.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, notificationIconData))
        {
            throw new Exception("Failed to modify icon in the notification area.");
        }
        _icon = newIcon;
    }
}