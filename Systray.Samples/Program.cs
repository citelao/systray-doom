using Systray;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

Console.WriteLine("Hello, World!");

using var messageWindow = new MessageOnlyWindow("Systray.Sample.Window", (hwnd, msg, wParam, lParam) =>
{
    switch (msg)
    {
        case PInvoke.WM_CLOSE:
            PInvoke.DestroyWindow(hwnd);
            return new LRESULT(0);

        case PInvoke.WM_DESTROY:
            PInvoke.PostQuitMessage(0);
            return new LRESULT(0);

        default:
            return null;
    }
});

// Wait for enter key
Console.WriteLine("Press Enter to exit...");
Console.ReadLine();
