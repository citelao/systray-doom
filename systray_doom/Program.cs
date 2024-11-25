using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using System.Diagnostics;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.WinRT;
using Windows.Win32.System.WinRT.Composition;
using static Crayon.Output;
using Windows.UI.Composition;
using Windows.UI.Composition.Desktop;
using WinRT;
using System.Numerics;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

Console.WriteLine("Starting doom...");

var i = PInvokeDoom.rust_function();
Console.WriteLine(Dim($"Testing Rust connection: {i == 42} ({i})"));

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
        MenuHelpers.InsertMenuItem(menu, 3, new MenuItemInfoBuilder { Text = "&Open window", Id = 3, Default = true }.Build());
        MenuHelpers.InsertMenuItem(menu, 4, new MenuItemInfoBuilder { Text = "E&xit", Id = 4 }.Build());

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
            // Open window
            PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_SHOWNORMAL);
            PInvoke.SetForegroundWindow(hwnd);
        }
        else if (response == 4)
        {
            // Exit
            Doom.Stop();
            PInvoke.PostMessage(hwnd, PInvoke.WM_CLOSE, 0, 0);
        }

        // TODO: return focus to systray after dismissing the menu. This line
        // doesn't work:
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

var taskbarCreatedWindowMessage = PInvoke.RegisterWindowMessage("TaskbarCreated");
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

        case PInvoke.WM_PAINT:
            Console.WriteLine("Painting...");
            // var hdc = PInvoke.BeginPaint(hwnd, out var ps);

            // // https://stackoverflow.com/a/1760571/788168
            // var icon = Doom.LastIcon;
            // if (!icon.IsNull)
            // {
            //     var hdcIcon = PInvoke.CreateCompatibleDC(hdc);
            //     var oldIcon = PInvoke.SelectObject(hdcIcon, new NoReleaseSafeHandle((int)icon.Value));
            //     PInvoke.BitBlt(hdc, 0, 0, 16, 16, hdcIcon, 0, 0, ROP_CODE.SRCCOPY);
            //     PInvoke.SelectObject(hdcIcon, oldIcon);
            //     PInvoke.DeleteDC(hdcIcon);
            // }

            // PInvoke.EndPaint(hwnd, ps);
            break;

        case PInvoke.WM_SIZE:
            var isMinimize = wParam == PInvoke.SIZE_MINIMIZED;
            if (isMinimize)
            {
                PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_HIDE);

                // TODO: doesn't work.
                PInvokeHelpers.THROW_IF_FALSE(PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETFOCUS, new TrayIconMessageBuilder(guid: Constants.SystrayGuid).Build()));
            }
            PInvokeHelpers.THROW_IF_FALSE(PInvoke.UpdateWindow(hwnd));
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
                    Console.WriteLine(Dim($"Tray icon context menu for {iconId} ({x}, {y})."));
                    // var pt = new Point(x, y);
                    // var client = PInvoke.ScreenToClient(hwnd, ref pt);
                    // Console.WriteLine($"Client: {pt.X}, {pt.Y}");
                    TryDisplayContextMenu(hwnd, x, y);
                    break;

                case PInvoke.WM_MOUSEMOVE:
                    Console.WriteLine(Dim($"Tray icon mouse move for {iconId} ({x}, {y})."));
                    break;

                case PInvoke.WM_LBUTTONDOWN:
                    Console.WriteLine(Dim($"Tray icon left button down for {iconId} ({x}, {y})."));
                    break;

                case PInvoke.WM_LBUTTONUP:
                    Console.WriteLine(Dim($"Tray icon left button up for {iconId} ({x}, {y})."));
                    break;

                case PInvoke.WM_LBUTTONDBLCLK:
                    Console.WriteLine(Dim($"Tray icon left button double click for {iconId} ({x}, {y})."));
                    break;

                case PInvoke.WM_RBUTTONDOWN:
                    Console.WriteLine(Dim($"Tray icon right button down for {iconId} ({x}, {y})."));
                    break;

                case PInvoke.WM_RBUTTONUP:
                    Console.WriteLine(Dim($"Tray icon right button up for {iconId} ({x}, {y})."));
                    break;

                case PInvoke.WM_MBUTTONDOWN:
                    Console.WriteLine(Dim($"Tray icon middle button down for {iconId} ({x}, {y})."));
                    break;

                case PInvoke.WM_MBUTTONUP:
                    Console.WriteLine(Dim($"Tray icon middle button up for {iconId} ({x}, {y})."));
                    break;

                case PInvoke.NIN_SELECT:
                    Console.WriteLine(Dim($"Tray icon select for {iconId} ({x}, {y})."));
                    PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_SHOWNORMAL);
                    PInvoke.SetForegroundWindow(hwnd);
                    break;

                case PInvoke.NIN_BALLOONSHOW:
                    Console.WriteLine(Dim($"Tray icon balloon show for {iconId} ({x}, {y})."));
                    break;

                case PInvoke.NIN_BALLOONHIDE:
                    Console.WriteLine(Dim($"Tray icon balloon hide for {iconId} ({x}, {y})."));
                    break;

                case PInvoke.NIN_BALLOONTIMEOUT:
                    Console.WriteLine(Dim($"Tray icon balloon timeout for {iconId} ({x}, {y})."));
                    break;

                case PInvoke.NIN_BALLOONUSERCLICK:
                    Console.WriteLine(Dim($"Tray icon balloon user click for {iconId} ({x}, {y})."));
                    break;

                case PInvoke.NIN_POPUPOPEN:
                    Console.WriteLine(Dim($"Tray icon popup open for {iconId} ({x}, {y})."));
                    break;

                case PInvoke.NIN_POPUPCLOSE:
                    Console.WriteLine(Dim($"Tray icon popup close for {iconId} ({x}, {y})."));
                    break;

                default:
                    Console.WriteLine(Dim($"Tray icon message: {ev}"));
                    break;
            }

            break;

        default:
            if (msg == taskbarCreatedWindowMessage)
            {
                // Fired if Explorer crashes & restarts, or if the primary
                // display DPI changes.
                //
                // https://learn.microsoft.com/en-us/windows/win32/shell/taskbar#taskbar-creation-notification
                Console.WriteLine(Dim("Taskbar created message received."));
                // TODO: recreate icon.
            }
            else
            {
                Console.WriteLine(Dim($"WindowProc: {msg} {wParam} {lParam}"));
            }
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
            // You need to include the size of this sWindows.Win32.UI.WindowsAndMessaging.truct.
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
        0, // dwExStyle WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP
        WindowClassName,
        "Systray Doom - v0.0.1", // lpWindowName
        WINDOW_STYLE.WS_OVERLAPPEDWINDOW, // dwStyle
        PInvoke.CW_USEDEFAULT, // x
        PInvoke.CW_USEDEFAULT, // y
        640, // nWidth
        480, // nHeight
        HWND.Null, // hWndParent
        new NoReleaseSafeHandle(0), // hMenu
        new NoReleaseSafeHandle(0), // hInstance
        &data // lpParam
    );
}

// https://github.com/microsoft/CsWin32/blob/58e949951dbcba2a84a35158bb10ff89beb2300d/test/WinRTInteropTest/CompositionHost.cs#L84
var options = new DispatcherQueueOptions()
{
    dwSize = (uint)Marshal.SizeOf<DispatcherQueueOptions>(),
    apartmentType = DISPATCHERQUEUE_THREAD_APARTMENTTYPE.DQTAT_COM_STA,
    threadType = DISPATCHERQUEUE_THREAD_TYPE.DQTYPE_THREAD_CURRENT,
};
PInvoke.CreateDispatcherQueueController(options, out var controller).ThrowOnFailure();
var compositor = new Compositor();
var interop = compositor.As<ICompositorDesktopInterop>() ?? throw new InvalidOperationException("ICompositorDesktopInterop not supported.");
interop.CreateDesktopWindowTarget(hwnd, false, out var target);

var root = compositor.CreateContainerVisual();
root.RelativeSizeAdjustment = Vector2.One;
root.Offset = new Vector3(124, 12, 0);
target.Root = root;

// Microsoft.UI.DispatchQueue.GetForCurrentThread().TryEnqueue(() =>
// {
//     var app = new Microsoft.UI.Xaml.Application();
//     app.OnLaunched += (s, e) =>
//     {
//         var window = new Microsoft.UI.Xaml.Window();
//         window.Activate();
//     };
//     app.Start();
// });

var element = compositor.CreateSpriteVisual();
var color = new Windows.UI.Color { R = 0, G = 0, B = 255, A = 255 };
element.Brush = compositor.CreateColorBrush(color);
element.Size = new Vector2(100, 100);
root.Children.InsertAtTop(element);

var trayIcon = new TrayIcon(Constants.SystrayGuid, hwnd, trayIconMessage)
{
    Tooltip = "Hello, Windows!"
};

var doomTask = Doom.RunAsync();

// TODO: we can't use LoadedImageSurface because it's XAML.
// https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.media.loadedimagesurface?view=winrt-26100
//
// await Task.Delay(100);
// IRandomAccessStream memoryStream = new InMemoryRandomAccessStream();
// var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, memoryStream);
// encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)Doom.DesiredSizePx.height, (uint)Doom.DesiredSizePx.width, 96, 96, Doom.LastRgbaFrame);
// await encoder.FlushAsync();

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