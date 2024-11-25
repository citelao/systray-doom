using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using System.Diagnostics;
using Windows.Win32.UI.HiDpi;

Console.WriteLine("Hello, World!");

var i = PInvokeDoom.rust_function();
Console.WriteLine(i);

// Heavily inspired by https://github.com/microsoft/CsWin32/blob/99ddd314ea359d3a97afa82c735b6a25eb25ea32/test/WinRTInteropTest/Program.cs

// Turn on DPI awareness the non-recommended way! Since it's easier than a manifest! Don't trust
// any code in this file!
//
// https://learn.microsoft.com/en-us/windows/win32/hidpi/setting-the-default-dpi-awareness-for-a-process
// https://stackoverflow.com/questions/23551112/how-can-i-set-the-dpiaware-property-in-a-windows-application-manifest-to-per-mo/
PInvokeHelpers.THROW_IF_FALSE(PInvoke.SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2));

const string WindowClassName = "SimpleSystrayWindow";
var trayIconMessage = PInvoke.RegisterWindowMessage("DoomTaskbarWM");

// Use the variable style function here to get easy access to the full function
// signature. This function isn't used directly.
// WNDPROC intermediateWndProc = (hwnd, msg, wParam, lParam) =>
// {
//     return WndProc(hwnd, msg, wParam, lParam);
// };

bool TryDisplayContextMenu(HWND hwnd, int x, int y)
{
    // https://github.com/microsoft/Windows-classic-samples/blob/d338bb385b1ac47073e3540dbfa810f4dcb12ed8/Samples/Win7Samples/winui/shell/appshellintegration/NotificationIcon/NotificationIcon.cpp#L217
    PInvoke.SetForegroundWindow(hwnd);

    // If you ... happen to call `CreateMenu` instead here, you'll get a menu
    // that has basically no width.
    var menu = PInvoke.CreatePopupMenu();
    try
    {
        MenuHelpers.InsertMenuItem(menu, 0, new MenuItemInfoBuilder { Text = "Systray Doom", Enabled = false }.Build());
        MenuHelpers.InsertMenuItem(menu, 1, new MenuItemInfoBuilder { Text = "By Ben Stolovitz", Enabled = false }.Build());
        MenuHelpers.InsertMenuItem(menu, 2, MenuItemInfoBuilder.CreateSeparator());
        MenuHelpers.InsertMenuItem(menu, 3, MenuItemInfoBuilder.CreateString("E&xit", id: 3).Build());

        // TODO: docs say to use this, but there are no examples.
        // PInvokeHelpers.THROW_IF_FALSE(PInvoke.CalculatePopupWindowPosition(
        //     new POINT(x, y),
        //     new RECT(0, 0, 0, 0),
        //     TPM.TPM_VERTICAL | TPM.TPM_RIGHTALIGN | TPM.TPM_RIGHTBUTTON,
        //     out var position
        // ));

        // Get alignment flags & also ensure that:
        // 1. The menu returns the command ID of the item selected.
        // 2. The menu does not send notifications of the selected item to
        //    parent HWND.
        var flags = MenuHelpers.GetPopupAlignmentFlags();
        var returnValueFlags = TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD | TRACK_POPUP_MENU_FLAGS.TPM_NONOTIFY;
        flags |= returnValueFlags;

        // TODO: what DPI/coordinate space are X & Y? (they are "screen
        // coordinates", but I think they correspond to the app's DPI, whereas I
        // think the x & y we get from the WMs are in the system DPI. Before I
        // turned on DPI awareness, this menu drew in the bottom-right corner of
        // the screen).
        // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-trackpopupmenuex
        // https://learn.microsoft.com/en-us/windows/win32/learnwin32/dpi-and-device-independent-pixels
        //
        // TODO: TPM_LAYOUTRTL on RTL systems?

        var response = PInvoke.TrackPopupMenuEx(
            new NoReleaseSafeHandle((int)menu.Value),
            (uint)(flags),
            x,
            y,
            hwnd,
            null);
        if (response == 0)
        {
            // Either nothing selected or an error occurred.
            // throw new Exception("TrackPopupMenuEx failed.");
        }
        Console.WriteLine($"TrackPopupMenuEx complete; {response}");

        if (response == 3)
        {
            // Exit
            Doom.Stop();
            PInvoke.PostMessage(hwnd, PInvoke.WM_CLOSE, 0, 0);
        }

        // TODO: doesn't work...
        PInvokeHelpers.THROW_IF_FALSE(PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETFOCUS, new TrayIconMessageBuilder(guid: Constants.SystrayGuid).Build()));
    }
    finally
    {
        var result = PInvoke.DestroyMenu(menu);
        if (!result)
        {
            // throw new Exception("Failed to destroy menu.");
            Console.Error.WriteLine("Failed to destroy menu.");
        }
    }

    return false;
}

var windowProcHelper = new WindowMessageHandler((hwnd, msg, wParam, lParam) =>
{
    switch (msg)
    {
        case PInvoke.WM_CLOSE:
            PInvoke.DestroyWindow(hwnd);
            break;

        case PInvoke.WM_DESTROY:
            PInvoke.PostQuitMessage(0);
            break;

        // https://stackoverflow.com/a/65642709/788168
        case var value when value == trayIconMessage:
            // Console.WriteLine("Tray icon message received.");
            var ev = (uint)PInvokeHelpers.LOWORD(lParam.Value);
            var iconId = (uint)PInvokeHelpers.HIWORD(lParam.Value);
            var x = PInvokeHelpers.GET_X_LPARAM(wParam.Value);
            var y = PInvokeHelpers.GET_Y_LPARAM(wParam.Value);

            // https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyicona#remarks
            // https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-notifyicondataa#:~:text=but%20the%20interpretation%20of%20the%20lParam%20and%20wParam%20parameters%20of%20that%20message%20is%20changed%20as%20follows%3A
            switch (ev)
            {
                case PInvoke.WM_CONTEXTMENU:
                    Console.WriteLine($"Tray icon context menu for {iconId} ({x}, {y}).");
                    // var pt = new Point(x, y);
                    // var client = PInvoke.ScreenToClient(hwnd, ref pt);
                    // Console.WriteLine($"Client: {pt.X}, {pt.Y}");
                    TryDisplayContextMenu(hwnd, x, y);
                    break;

                case PInvoke.WM_MOUSEMOVE:
                    Console.WriteLine($"Tray icon mouse move for {iconId} ({x}, {y}).");
                    break;

                case PInvoke.WM_LBUTTONDOWN:
                    Console.WriteLine($"Tray icon left button down for {iconId} ({x}, {y}).");
                    break;

                case PInvoke.WM_LBUTTONUP:
                    Console.WriteLine($"Tray icon left button up for {iconId} ({x}, {y}).");
                    break;
                
                case PInvoke.WM_LBUTTONDBLCLK:
                    Console.WriteLine($"Tray icon left button double click for {iconId} ({x}, {y}).");
                    break;

                case PInvoke.WM_RBUTTONDOWN:
                    Console.WriteLine($"Tray icon right button down for {iconId} ({x}, {y}).");
                    break;

                case PInvoke.WM_RBUTTONUP:
                    Console.WriteLine($"Tray icon right button up for {iconId} ({x}, {y}).");
                    break;

                case PInvoke.NIN_SELECT:
                    Console.WriteLine($"Tray icon select for {iconId} ({x}, {y}).");
                    break;

                case PInvoke.NIN_BALLOONSHOW:
                    Console.WriteLine($"Tray icon balloon show for {iconId} ({x}, {y}).");
                    break;

                case PInvoke.NIN_BALLOONHIDE:
                    Console.WriteLine($"Tray icon balloon hide for {iconId} ({x}, {y}).");
                    break;

                case PInvoke.NIN_BALLOONTIMEOUT:
                    Console.WriteLine($"Tray icon balloon timeout for {iconId} ({x}, {y}).");
                    break;

                case PInvoke.NIN_BALLOONUSERCLICK:
                    Console.WriteLine($"Tray icon balloon user click for {iconId} ({x}, {y}).");
                    break;

                case PInvoke.NIN_POPUPOPEN:
                    Console.WriteLine($"Tray icon popup open for {iconId} ({x}, {y}).");
                    break;

                case PInvoke.NIN_POPUPCLOSE:
                    Console.WriteLine($"Tray icon popup close for {iconId} ({x}, {y}).");
                    break;

                default:
                    Console.WriteLine($"Tray icon message: {ev}");
                    break;
            }

            break;

        default:
            Console.WriteLine($"WindowProc: {msg} {wParam} {lParam}");
            break;
    }

    return null;
});

unsafe
{
    fixed (char* pClassName = WindowClassName)
    {
        var wndClass = new WNDCLASSEXW
        {
            // You need to include the size of this struct.
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),

            // Seems to work without this, but it seems sketchy to leave it out.
            hInstance = PInvoke.GetModuleHandle(default(PCWSTR)),

            // Required to actually run your WndProc (and the app will crash if
            // null).
            lpfnWndProc = WindowMessageHandler.StaticWndProc,

            // Required to identify the window class in CreateWindowEx.
            lpszClassName = pClassName,
        };

        // We ignore the returned class atom & use the class name directly.
        // https://devblogs.microsoft.com/oldnewthing/20080501-00/?p=22503
        PInvoke.RegisterClassEx(wndClass);
    }
}

var data = windowProcHelper.LpParamData();
HWND hwnd;
unsafe
{
    hwnd = PInvoke.CreateWindowEx(
        0, // dwExStyle
        WindowClassName,
        "Hello, Windows!", // lpWindowName
        0, // dwStyle
        0, // x
        0, // y
        640, // nWidth
        480, // nHeight
        HWND.Null, // hWndParent
        new NoReleaseSafeHandle(0), // hMenu
        new NoReleaseSafeHandle(0), // hInstance
        &data // lpParam
    );
}

var trayIcon = new TrayIcon(Constants.SystrayGuid, hwnd, trayIconMessage)
{
    Tooltip = "Hello, Windows!"
};

var doomTask = Doom.RunAsync();

Console.WriteLine("Starting message loop...");
Console.WriteLine("Press Ctrl-C to exit.");

while (PInvoke.GetMessage(out var msg, HWND.Null, 0, 0))
{
    PInvoke.TranslateMessage(msg);
    PInvoke.DispatchMessage(msg);
}

Console.WriteLine("Exiting...");

// Don't await at all! Exit the app.
// Old: Await synchronously to avoid a CS9123 because of our reference to &data.
// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/warning-waves
// doomTask.GetAwaiter().GetResult();
// Older: await doomTask;

class WindowMessageHandler
{
    public static LRESULT StaticWndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case PInvoke.WM_NCCREATE:
                unsafe
                {
                    var createStruct = (CREATESTRUCTW*)lParam.Value;
                    var data = (Data*)createStruct->lpCreateParams;
                    PInvoke.SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA, (nint)data);
                }
                return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);

            default:
                unsafe
                {
                    var data = (Data*)PInvoke.GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA);
                    if (data != null)
                    {
                        var that = Marshal.GetDelegateForFunctionPointer<WndProcDelegate>(data->WndProcDelegate);
                        var result = that(hwnd, msg, wParam, lParam);
                        if (result != null)
                        {
                            return result.Value;
                        }
                    }
                }

                return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
        }
    }

    public delegate LRESULT? WndProcDelegate(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam);

    public struct Data
    {
        public nint WndProcDelegate;
    }
    private Data _data;

    private readonly WndProcDelegate _wndProc;

    public WindowMessageHandler(WndProcDelegate del)
    {
        // https://github.com/ControlzEx/ControlzEx/blob/cbb56cab39ffc78d9599208826f47eeab70455f7/src/ControlzEx/Controls/GlowWindow.cs#L94
        _wndProc = del;
        var delPtr = Marshal.GetFunctionPointerForDelegate(del);
        _data = new Data
        {
            WndProcDelegate = delPtr,
        };
    }

    public Data LpParamData()
    {
        return _data;
    }
}