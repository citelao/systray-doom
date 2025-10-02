namespace Systray;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Systray.NativeTypes;
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

    internal static WndProcDelegate ToInternalDelegate(UserDelegate deleg)
    {
        return (HWND hwnd, uint msg, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam) =>
        {
            var result = deleg(new(hwnd), msg, new(wParam.Value), new(lParam.Value));
            return result?.ToWin32() ?? new Windows.Win32.Foundation.LRESULT(0);
        };
    }

    // Store all known handlers, mapped by HWND. We use a WeakReference: if your
    // Handler gets GC'd, we stop calling your delegate.
    private unsafe struct HandlerInfo
    {
        public WeakReference<WindowSubclassHandler> Handler;
        public delegate* unmanaged[Cdecl]<nint, uint, nuint, nint, nint> OriginalWndProc;
    }
    private readonly static Dictionary<HWND, HandlerInfo> s_handlers = [];

    private readonly WndProcDelegate Delegate;

    public WindowSubclassHandler(NoReleaseHwnd hwnd, UserDelegate wndProc)
        : this(hwnd, ToInternalDelegate(wndProc))
    {
        // No addl work.
    }

    internal unsafe WindowSubclassHandler(NoReleaseHwnd hwnd, WndProcDelegate wndProc)
    {
        if (s_handlers.ContainsKey(hwnd.ToHwnd()))
        {
            // We could handle this by storing a list of delegates, but defer
            // that for now.
            throw new InvalidOperationException("This window is already subclassed via WindowSubclassHandler");
        }

        Delegate = wndProc;

        var originalWndProc = PInvokeCore.GetWindowLong(hwnd.ToHwnd(), WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC);
        s_handlers[hwnd.ToHwnd()] = new HandlerInfo()
        {
            Handler = new(this, trackResurrection: false),
            OriginalWndProc = (delegate* unmanaged[Cdecl]<nint, uint, nuint, nint, nint>)originalWndProc,
        };

        delegate* unmanaged[Cdecl]<HWND, uint, Windows.Win32.Foundation.WPARAM, Windows.Win32.Foundation.LPARAM, nint> wndProcPointer = &SubclassWndProc;
        var otherWndProc = PInvokeCore.SetWindowLong(hwnd.ToHwnd(), WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC, (nint)wndProcPointer);
        Debug.Assert(otherWndProc == originalWndProc);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe nint SubclassWndProc(HWND hwnd, uint msg, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam)
    {
        if(s_handlers.TryGetValue(hwnd, out var handlerInfo))
        {
            // Attempt to call the user's delegate.
            var handler = handlerInfo.Handler.TryGetTarget(out var target) ? target : null;
            var result = handler?.Delegate.Invoke(hwnd, msg, wParam, lParam);
            if (result != null)
            {
                return result.Value;
            }

            // If no delegate or the delegate chose not to handle the message,
            // fall back to the original WndProc.
            var originalResult = CallWindowProc(handlerInfo.OriginalWndProc, hwnd, msg, wParam, lParam);
            return originalResult;
        }
        else
        {
            // We never remove items from s_handlers, so this should never happen.
            throw new InvalidOperationException("No handler registered for this window");
        }
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
