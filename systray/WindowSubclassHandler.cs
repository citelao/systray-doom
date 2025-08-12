namespace Systray;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Systray.NativeTypes;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

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

    // We have issues casting the WNDPROC function pointer into a WNDPROC
    // delegate, since the type ends up conflicting with the systray_doom
    // version, so simply implement a generic P/Invoke version of CallWindowProc.
    //
    // Taken from CsWin32 generated code.
    [DllImport("USER32.dll", ExactSpelling = true, EntryPoint = "CallWindowProcW")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("windows5.0")]
    internal static extern LRESULT CallWindowProc(Delegate lpPrevWndFunc, HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam);

    internal WindowSubclassHandler(NoReleaseHwnd hwnd, WndProcDelegateInternal wndProc)
    {
        var originalWndProc = PInvokeCore.GetWindowLong(hwnd.AsHWND(), WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC);
        var originalWndProcDel = Marshal.GetDelegateForFunctionPointer(originalWndProc, typeof(WNDPROC))!;
        _delegate = (hwnd, msg, wParam, lParam) =>
        {
            var result = wndProc(hwnd, msg, wParam, lParam);
            if (result != null)
            {
                return result.Value;
            }
            return CallWindowProc(originalWndProcDel, hwnd, msg, wParam, lParam);
        };

        var otherWndProc = PInvokeCore.SetWindowLong(hwnd.AsHWND(), WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_delegate));
        Debug.Assert(otherWndProc == originalWndProc);
    }
}