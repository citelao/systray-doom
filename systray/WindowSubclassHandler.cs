namespace Systray;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Systray.NativeTypes;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

// Yup, it's a very bare-bones interface :)
public interface IWindowSubclassHandler
{
}

public partial class WindowSubclassHandler : IWindowSubclassHandler
{
    // This is the easiest way to expose WindowSubclassHandler publicly
    // *without* making it harder to call internally. Simply expose a public
    // delegate with the public types, but allow use of the internal types
    // directly.
    public delegate NativeTypes.LRESULT? UserDelegate(NoReleaseHwnd hwnd, uint msg, NativeTypes.WPARAM wParam, NativeTypes.LPARAM lParam);
    internal delegate Windows.Win32.Foundation.LRESULT? WndProcDelegate(HWND hwnd, uint msg, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam);
    internal delegate nint TrueWndProcDelegate(nint hwnd, uint msg, nuint wParam, nint lParam);

    internal static WndProcDelegate ToInternalDelegate(UserDelegate del)
    {
        return (HWND hwnd, uint msg, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam) =>
        {
            var result = del(new(hwnd), msg, new(wParam.Value), new(lParam.Value));
            return result?.ToWin32() ?? new Windows.Win32.Foundation.LRESULT(0);
        };
    }

    private readonly TrueWndProcDelegate _delegate;

    public WindowSubclassHandler(NoReleaseHwnd hwnd, UserDelegate wndProc)
        : this(hwnd, ToInternalDelegate(wndProc))
    {
        // No addl work.
    }

    // TODO: destructor. Needs to handle other folks subclassing our window.
    internal unsafe WindowSubclassHandler(NoReleaseHwnd hwnd, WndProcDelegate wndProc)
    {
        var originalWndProc = PInvokeCore.GetWindowLong(hwnd.ToHwnd(), WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC);
        // Console.WriteLine($"Original WndProc: 0x{originalWndProc:X}");

        _delegate = (hwnd, msg, wParam, lParam) =>
        {
            // Console.WriteLine($"In subclassed WndProc: msg=0x{msg:X}, wParam=0x{wParam:X}, lParam=0x{lParam:X}");
            var result = wndProc(new(hwnd), msg, new(wParam), new(lParam));
            if (result != null)
            {
                return new(result.Value);
            }
            var originalResult = CallWindowProc((delegate* unmanaged[Cdecl]<nint, uint, nuint, nint, nint>)originalWndProc, hwnd, msg, wParam, lParam);
            return new(originalResult);
        };

        // Console.WriteLine($"New WndProc: 0x{Marshal.GetFunctionPointerForDelegate(_delegate).ToInt64():X}");

        var otherWndProc = PInvokeCore.SetWindowLong(hwnd.ToHwnd(), WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_delegate));
        Debug.Assert(otherWndProc == originalWndProc);
    }

    // We have issues casting the WNDPROC function pointer into a WNDPROC
    // delegate, since the type ends up conflicting with the systray_doom
    // version, so simply implement a generic P/Invoke version of CallWindowProc.
    //
    // Taken from CsWin32 generated code.
    [LibraryImport("USER32.dll", EntryPoint = "CallWindowProcW")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("windows5.0")]
    private static unsafe partial nint CallWindowProc(delegate* unmanaged[Cdecl]<nint, uint, nuint, nint, nint> lpPrevWndFunc, IntPtr hWnd, uint Msg, nuint wParam, nint lParam);
}
