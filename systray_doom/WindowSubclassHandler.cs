using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

using static Crayon.Output;

internal class WindowSubclassHandler
{
    private readonly WNDPROC _delegate;

    // TODO: destructor
    public WindowSubclassHandler(HWND hwnd, WindowMessageHandler.WndProcDelegate wndProc)
    {
        var originalWndProc = PInvoke.GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC);
        var originalWndProcDel = Marshal.GetDelegateForFunctionPointer<WNDPROC>(originalWndProc);
        _delegate = (hwnd, msg, wParam, lParam) =>
        {
            var result = wndProc(hwnd, msg, wParam, lParam);
            if (result != null)
            {
                return result.Value;
            }
            return PInvoke.CallWindowProc(originalWndProcDel, hwnd, msg, wParam, lParam);
        };

        var otherWndProc = PInvoke.SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_delegate));
        Debug.Assert(otherWndProc == originalWndProc);
    }
}