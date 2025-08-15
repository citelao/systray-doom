namespace Systray;

using System.Drawing;
using System.Runtime.InteropServices;
using Systray.NativeTypes;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

using static Crayon.Output;

/// <summary>
/// A systray icon.
/// </summary>
public class TrayIcon
{
    /// <summary>
    /// Tooltip to display when hovering the icon, also used as its accessible
    /// name. Will be truncated to 128 characters (including the terminating
    /// null).
    /// </summary>
    public string Tooltip
    {
        set { SetTooltip(value); }
        get { return _tooltip; }
    }
    private string _tooltip = string.Empty;

    /// <summary>
    /// Icon to display in the systray. Defaults to the application icon.
    /// </summary>
    public NoReleaseHicon Icon
    {
        set { SetIcon(value); }
        get { return _icon; }
    }
    private NoReleaseHicon _icon = new(PInvokeSystray.LoadIcon(HINSTANCE.Null, PInvokeSystray.IDI_APPLICATION));

    /// <summary>
    /// Guid to uniquely identify this (class of) tray icon. Used to preserve
    /// pinned state for this icon, so make sure to use a consistent one for
    /// your app.
    /// </summary>
    public readonly Guid Guid;

    /// <summary>
    /// The owning HWND for this tray icon. Handles messages.
    /// </summary>
    public readonly NoReleaseHwnd OwnerHwnd;

    /// <summary>
    /// The callback message to use for this tray icon. If null, icon will not
    /// send any messages.
    /// </summary>
    public readonly uint? CallbackMessage = null;

    //
    // EVENT HANDLERS!
    //

    /// <summary>
    /// Context menu callback. Fired when a user right-clicks on the systray icon
    /// or presses Shift-F10 with the icon focused.
    ///
    /// Includes the clicked coordinate: this is *always* in physical pixels &
    /// screen coordinates, even if your app is not DPI-aware.
    ///
    /// Return true to indicate that the message was handled.
    /// </summary>
    public ContextMenuHandler? ContextMenu;
    public delegate bool ContextMenuHandler(NoReleaseHwnd hwnd, PhysicalPoint pt);

    /// <summary>
    /// Select callback. Fired when a user clicks the systray icon or presses
    /// Enter with the icon focused.
    ///
    /// Includes the clicked coordinate: this is *always* in physical pixels &
    /// screen coordinates, even if your app is not DPI-aware.
    ///
    /// Return true to indicate that the message was handled.
    /// </summary>
    public SelectHandler? Select;
    public delegate bool SelectHandler(NoReleaseHwnd hwnd, PhysicalPoint pt);

    /// <summary>
    /// Mouse move callback. Fired when the mouse moves over the systray icon.
    ///
    /// Includes the hovered coordinate: this is *always* in physical pixels &
    /// screen coordinates, even if your app is not DPI-aware.
    ///
    /// Return true to indicate that the message was handled.
    /// </summary>
    public MouseMoveHandler? MouseMove;
    public delegate bool MouseMoveHandler(NoReleaseHwnd hwnd, Point point);

    // public delegate LRESULT? CallbackMessageHandlerDelegate(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam);
    // public CallbackMessageHandlerDelegate? CallbackMessageHandler;

    // Delegate exposed for testing.
    internal delegate BOOL Shell_NotifyIconDelegate(NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData);
    internal static Shell_NotifyIconDelegate Shell_NotifyIconFn = NotifyIcon.Shell_NotifyIcon;

    // Factory delegate exposed for testing.
    internal delegate IWindowSubclassHandler WindowSubclassHandlerFactoryDelegate(NoReleaseHwnd hwnd, WindowSubclassHandler.WndProcDelegate wndProc);
    internal static WindowSubclassHandlerFactoryDelegate WindowSubclassHandlerFactoryFn = (hwnd, wndProc) => new WindowSubclassHandler(hwnd, wndProc);

    // DefWindowProc delegate exposed for testing.
    internal delegate Windows.Win32.Foundation.LRESULT DefWindowProcDelegate(HWND hwnd, uint msg, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam);
    internal static DefWindowProcDelegate DefWindowProcFn = PInvokeSystray.DefWindowProc;

    // Hold a reference to the WindowSubclassHandler so it doesn't get GC'd; it
    // owns the window proc delegate.
    private readonly IWindowSubclassHandler? _windowSubclassHandler = null;

    // Fired if Explorer crashes & restarts, or if the primary display DPI changes.
    internal static readonly uint s_taskbarCreatedWindowMessage = PInvokeSystray.RegisterWindowMessage("TaskbarCreated");

    public TrayIcon(Guid guid, NoReleaseHwnd ownerHwnd, bool shouldHandleMessages = true, uint? callbackMessage = null)
    {
        Guid = guid;
        OwnerHwnd = ownerHwnd;

        if (shouldHandleMessages)
        {
            _windowSubclassHandler = WindowSubclassHandlerFactoryFn(ownerHwnd, HandleMessage);

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

    /// <summary>
    /// Re-create the icon; useful if Explorer crashes (though we handle that
    /// automatically).
    /// </summary>
    public void Create()
    {
        var notificationIconData = new TrayIconMessageBuilder(guid: Guid)
        {
            HWND = OwnerHwnd,
            Tooltip = _tooltip,
            Icon = _icon,
            CallbackMessage = CallbackMessage,
        }.Build();
        PInvokeHelpers.THROW_IF_FALSE(Shell_NotifyIconFn(NOTIFY_ICON_MESSAGE.NIM_ADD, notificationIconData), "Failed to add icon to the notification area.");
        PInvokeHelpers.THROW_IF_FALSE(Shell_NotifyIconFn(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, notificationIconData), "Failed to set version of icon in the notification area.");
    }

    /// <summary>
    /// *Should* set focus to the tray icon. Doesn't work.
    ///
    /// NIM_SETFOCUS seems completely broken on modern Windows 11. I cannot find
    /// a tray icon that moves focus back correctly. I wonder if this was a
    /// security change & the docs have not been updated?
    ///
    /// TODO: fix?
    /// </summary>
    public void Focus()
    {
        PInvokeHelpers.THROW_IF_FALSE(Shell_NotifyIconFn(
            NOTIFY_ICON_MESSAGE.NIM_SETFOCUS,
            new TrayIconMessageBuilder(guid: Guid).Build()));
    }

    private Windows.Win32.Foundation.LRESULT? HandleMessage(HWND hwnd, uint msg, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam)
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

                return new(DefWindowProcFn(hwnd, msg, wParam, lParam));

            case var tkwm when tkwm == s_taskbarCreatedWindowMessage:
                // Fired if Explorer crashes & restarts, or if the primary
                // display DPI changes.
                //
                // https://learn.microsoft.com/en-us/windows/win32/shell/taskbar#taskbar-creation-notification
                Console.WriteLine(Dim("Taskbar created message received."));

                // Re-create the icon.
                var notificationIconData = new TrayIconMessageBuilder(guid: Guid).Build();
                // PInvokeHelpers.THROW_IF_FALSE(Shell_NotifyIconFn(NOTIFY_ICON_MESSAGE.NIM_DELETE, notificationIconData), "Failed to add icon to the notification area.");
                Shell_NotifyIconFn(NOTIFY_ICON_MESSAGE.NIM_DELETE, notificationIconData);
                Create();

                // Don't replace the default window proc.
                return new(DefWindowProcFn(hwnd, msg, wParam, lParam));
        }
        return null;
    }

    // Parse & dispatch well-known messages.
    private Windows.Win32.Foundation.LRESULT? HandleCallbackMessage(HWND hwnd, uint msg, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam)
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
                return (ContextMenu?.Invoke(new(hwnd), new(x, y)) ?? false) ? new Windows.Win32.Foundation.LRESULT(0) : null;

            case PInvokeSystray.WM_MOUSEMOVE:
                Console.WriteLine(Dim($"Tray icon mouse move for {iconId} ({x}, {y})."));
                return (MouseMove?.Invoke(new(hwnd), new(x, y)) ?? false) ? new Windows.Win32.Foundation.LRESULT(0) : null;

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
                return (Select?.Invoke(new(hwnd), new(x, y)) ?? false) ? new Windows.Win32.Foundation.LRESULT(0) : null;

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
        if (!Shell_NotifyIconFn(NOTIFY_ICON_MESSAGE.NIM_MODIFY, notificationIconData))
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
        if (!Shell_NotifyIconFn(NOTIFY_ICON_MESSAGE.NIM_MODIFY, notificationIconData))
        {
            throw new Exception("Failed to modify icon in the notification area.");
        }
        _icon = newIcon;
    }
}