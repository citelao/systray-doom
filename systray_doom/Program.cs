using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Graphics.Gdi;
using System.Diagnostics;


Console.WriteLine("Hello, World!");

var i = PInvokeDoom.rust_function();
Console.WriteLine(i);

// Heavily inspired by https://github.com/microsoft/CsWin32/blob/99ddd314ea359d3a97afa82c735b6a25eb25ea32/test/WinRTInteropTest/Program.cs

const string WindowClassName = "SimpleSystrayWindow";
var trayIconMessage = PInvoke.RegisterWindowMessage("DoomTaskbarWM");

// Use the variable style function here to get easy access to the full function
// signature. This function isn't used directly.
// WNDPROC intermediateWndProc = (hwnd, msg, wParam, lParam) =>
// {
//     return WndProc(hwnd, msg, wParam, lParam);
// };

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

            switch (ev)
            {
                case PInvoke.WM_CONTEXTMENU:
                    Console.WriteLine("Tray icon context menu.");
                    break;

                case PInvoke.WM_MOUSEMOVE:
                    Console.WriteLine("Tray icon mouse move.");
                    break;

                case PInvoke.WM_LBUTTONDOWN:
                    Console.WriteLine("Tray icon left button down.");
                    break;

                case PInvoke.WM_LBUTTONUP:
                    Console.WriteLine("Tray icon left button up.");
                    break;
                
                case PInvoke.WM_LBUTTONDBLCLK:
                    Console.WriteLine("Tray icon left button double click.");
                    break;

                case PInvoke.WM_RBUTTONDOWN:
                    Console.WriteLine("Tray icon right button down.");
                    break;

                case PInvoke.WM_RBUTTONUP:
                    Console.WriteLine("Tray icon right button up.");
                    break;

                case PInvoke.NIN_BALLOONSHOW:
                    Console.WriteLine("Tray icon balloon show.");
                    break;

                case PInvoke.NIN_BALLOONHIDE:
                    Console.WriteLine("Tray icon balloon hide.");
                    break;

                case PInvoke.NIN_BALLOONTIMEOUT:
                    Console.WriteLine("Tray icon balloon timeout.");
                    break;

                case PInvoke.NIN_BALLOONUSERCLICK:
                    Console.WriteLine("Tray icon balloon user click.");
                    break;

                case PInvoke.NIN_POPUPOPEN:
                    Console.WriteLine("Tray icon popup open.");
                    break;

                case PInvoke.NIN_POPUPCLOSE:
                    Console.WriteLine("Tray icon popup close.");
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

var guid = Guid.Parse("bc540dbe-f04e-4c1c-a5a0-01b32095b04c");
var trayIcon = new TrayIcon(guid, hwnd, trayIconMessage)
{
    Tooltip = "Hello, Windows!"
};

static unsafe void DrawFrame(UInt32* frame, nint xres, nint yres)
{
    // Console.WriteLine($"DrawFrame: {xres}x{yres}");
    // var desiredSizePx = (height: 200, width: 200);
    var desiredSizePx = (height: 320, width: 320);
    // var desiredSizePx = (height: 400, width: 300);
    // var desiredSizePx = (height: (int)yres, width: (int)xres);

    // Assert that the desired size is smaller than the actual size.
    Debug.Assert(desiredSizePx.width <= xres);
    Debug.Assert(desiredSizePx.height <= yres);

    var xRange = (
        min: (xres - desiredSizePx.width) / 2,
        max: (xres - desiredSizePx.width) / 2 + desiredSizePx.width
    );
    // var xRange = (
    //     min: 0,
    //     max: desiredSizePx.width
    // );
    var yRange = (
        min: 0,
        max: desiredSizePx.height
    );

    var rgbaPixelArray = new byte[desiredSizePx.width * desiredSizePx.height * 4];

    // Convert the frame pointer array to a managed array.
    var currentPixelIndex = 0;
    var totalPixels = xres * yres;
    for (var i = 0; i < totalPixels; i++)
    {
        var currentPosn = (x: i % xres, y: i / xres);
        if (currentPosn.x < xRange.min || currentPosn.x >= xRange.max)
        {
            continue;
        }
        if (currentPosn.y < yRange.min || currentPosn.y >= yRange.max)
        {
            continue;
        }
        // Console.WriteLine($"Current pixel: {i} {currentPosn.x}, {currentPosn.y}");

        var argb = frame[i];
        var r = (byte)((argb >> 16) & 0xFF);
        var g = (byte)((argb >> 8) & 0xFF);
        var b = (byte)((argb >> 0) & 0xFF);
        // Alpha seems to be opacity. Inverting it.
        var a = (byte)(255 - (argb >> 24) & 0xFF);
        rgbaPixelArray[currentPixelIndex++] = r;
        rgbaPixelArray[currentPixelIndex++] = g;
        rgbaPixelArray[currentPixelIndex++] = b;
        rgbaPixelArray[currentPixelIndex++] = a;

        // if (i == 0)
        // {
        //     Console.WriteLine($"First pixel: 0x{frame[i]:x} -> R: {rgbaPixelArray[i * 4 + 0]:x}, G: {rgbaPixelArray[i * 4 + 1]:x}, B: {rgbaPixelArray[i * 4 + 2]:x}, A: {rgbaPixelArray[i * 4 + 3]:x}");
        // }
    }

    // https://stackoverflow.com/a/537722/788168
    GCHandle pinnedArray = GCHandle.Alloc(rgbaPixelArray, GCHandleType.Pinned);
    IntPtr pointer = pinnedArray.AddrOfPinnedObject();

    var icon = PInvoke.CreateIcon(
        default(HINSTANCE),
        desiredSizePx.width,
        desiredSizePx.height,
        4,
        8,
        (byte*)pointer.ToPointer(),
        (byte*)pointer.ToPointer()
    );

    pinnedArray.Free();

    // trayIcon.Icon = icon;
    var guid = Guid.Parse("bc540dbe-f04e-4c1c-a5a0-01b32095b04c");
    var notificationIconData = new TrayIconMessageBuilder(guid: guid)
    {
        Icon = icon,
    }.Build();
    if (!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, notificationIconData))
    {
        throw new Exception("Failed to modify icon in the notification area.");
    }

    // Clean up the old icon.
    if (!State.LastIcon.IsNull)
    {
        PInvoke.DestroyIcon(State.LastIcon);
    }

    State.LastIcon = icon;
}
static unsafe PInvokeDoom.CKeyData* KeyCallback()
{
    // Console.WriteLine("KeyCallback");
    return null;
}
static unsafe void SetWindowTitle(byte* title, nint size)
{
    var titleString = System.Text.Encoding.UTF8.GetString(title, (int)size);
    Console.WriteLine($"SetWindowTitle: {titleString}");

    var guid = Guid.Parse("bc540dbe-f04e-4c1c-a5a0-01b32095b04c");
    var notificationIconData = new TrayIconMessageBuilder(guid: guid)
    {
        Tooltip = titleString,
    }.Build();
    if (!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, notificationIconData))
    {
        throw new Exception("Failed to modify icon in the notification area.");
    }
}

var doomTask = Task.Run(() => {
    unsafe
    {
        var game = PInvokeDoom.create_game(
            DrawFrame,
            KeyCallback,
            SetWindowTitle
        );

        PInvokeDoom.start_game(game);
    }
});

Console.WriteLine("Starting message loop...");
Console.WriteLine("Press Ctrl-C to exit.");

while (PInvoke.GetMessage(out var msg, HWND.Null, 0, 0))
{
    PInvoke.TranslateMessage(msg);
    PInvoke.DispatchMessage(msg);
}

// Await synchronously to avoid a CS9123 because of our reference to &data.
// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/warning-waves
doomTask.GetAwaiter().GetResult();
// await doomTask;

static class State
{
    public static HICON LastIcon = HICON.Null;
}

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