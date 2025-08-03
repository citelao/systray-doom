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
    // This is the easiest way to expose WindowSubclassHandler publicly
    // *without* making it harder to call internally. Simply expose a public
    // delegate with the public types, but allow use of the internal types
    // directly.
    internal delegate LRESULT? WndProcDelegateInternal(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam);
    public delegate Lresult? WndProcDelegate(NoReleaseHwnd hwnd, uint msg, Wparam wParam, Lparam lParam);

    internal static WndProcDelegateInternal ToInternalDelegate(WndProcDelegate del)
    {
        return (HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam) =>
        {
            var result = del(new(hwnd), msg, new(wParam), new(lParam));
            return result?.AsLRESULT();
        };
    }

    private readonly WNDPROC _delegate;

    // TODO: destructor. Needs to handle other folks subclassing our window.
    public WindowSubclassHandler(NoReleaseHwnd hwnd, WndProcDelegate wndProc)
        : this(hwnd, ToInternalDelegate(wndProc))
    {
        // No addl work.
    }

    internal WindowSubclassHandler(NoReleaseHwnd hwnd, WndProcDelegateInternal wndProc)
    {
        var originalWndProc = PInvokeSystray.GetWindowLongPtr(hwnd.AsHWND(), WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC);
        var originalWndProcDel = Marshal.GetDelegateForFunctionPointer<WNDPROC>(originalWndProc);
        _delegate = (hwnd, msg, wParam, lParam) =>
        {
            var result = wndProc(hwnd, msg, wParam, lParam);
            if (result != null)
            {
                return result.Value;
            }
            return PInvokeSystray.CallWindowProc(originalWndProcDel, hwnd, msg, wParam, lParam);
        };

        var otherWndProc = PInvokeSystray.SetWindowLongPtr(hwnd.AsHWND(), WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_delegate));
        Debug.Assert(otherWndProc == originalWndProc);
    }
}