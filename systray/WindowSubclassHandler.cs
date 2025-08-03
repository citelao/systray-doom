namespace Systray;

using System.Diagnostics;
using System.Runtime.InteropServices;
using Systray.NativeTypes;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

using static Crayon.Output;

public class WindowSubclassHandler
{
    private readonly WNDPROC _delegate;

    // TODO: destructor. Needs to handle other folks subclassing our window.
    public WindowSubclassHandler(NoReleaseHwnd hwnd, WindowMessageHandler.WndProcDelegate wndProc)
    {
        var originalWndProc = PInvokeSystray.GetWindowLongPtr(hwnd.AsHWND(), WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC);
        var originalWndProcDel = Marshal.GetDelegateForFunctionPointer<WNDPROC>(originalWndProc);
        _delegate = (hwnd, msg, wParam, lParam) =>
        {
            var result = wndProc(new(hwnd), msg, new(wParam), new(lParam));
            if (result != null)
            {
                return result.Value.AsLRESULT();
            }
            return PInvokeSystray.CallWindowProc(originalWndProcDel, hwnd, msg, wParam, lParam);
        };

        var otherWndProc = PInvokeSystray.SetWindowLongPtr(hwnd.AsHWND(), WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_delegate));
        Debug.Assert(otherWndProc == originalWndProc);
    }
}