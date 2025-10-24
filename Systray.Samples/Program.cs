using Systray;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

Console.WriteLine("Hello, World!");

var messageOnlyWndClassName = "MessageOnlyWindowClass";
unsafe {
    fixed (char* fixedName = messageOnlyWndClassName)
    {
        var wndClass = new WNDCLASSEXW
        {
            cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<WNDCLASSEXW>(),
            // lpfnWndProc = PInvoke.DefWindowProc,
            hInstance = PInvoke.GetModuleHandle(default(PCWSTR)),
            lpszClassName = fixedName,
        };

        // We ignore the returned class atom & use the class name directly.
        // https://devblogs.microsoft.com/oldnewthing/20080501-00/?p=22503
        var atom = PInvoke.RegisterClassEx(wndClass);
        PInvokeHelpers.THROW_LAST_ERROR_IF(atom == 0, "Failed to register window class");
    }
}

// https://stackoverflow.com/questions/4081334/using-createwindowex-to-make-a-message-only-window
// https://pinvoke.net/default.aspx/Constants/HWND_MESSAGE.html
var HWND_MESSAGE = new HWND(unchecked((nint)(-3)));
HWND hwnd;
unsafe
{
    hwnd = PInvoke.CreateWindowEx(
        0,
        messageOnlyWndClassName,
        lpWindowName: "message window",
        0,
        0,
        0,
        0,
        0,
        HWND_MESSAGE,
        default,
        default,
        null);
}