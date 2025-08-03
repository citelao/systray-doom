namespace Systray;

using System.Runtime.InteropServices;
using Systray.NativeTypes;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

public class WindowMessageHandler
{
    // Nope: return types aren't compatible
    // public static nint StaticWndProc2(IntPtr hwnd, uint msg, nuint wparam, nint lparam)
    // {
    //     return 0;
    // }

    public static LRESULT StaticWndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case PInvokeSystray.WM_NCCREATE:
                unsafe
                {
                    var createStruct = (CREATESTRUCTW*)lParam.Value;
                    var data = (Data*)createStruct->lpCreateParams;
                    PInvokeSystray.SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA, (nint)data);
                }
                return PInvokeSystray.DefWindowProc(hwnd, msg, wParam, lParam);

            default:
                unsafe
                {
                    var data = (Data*)PInvokeSystray.GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA);
                    if (data != null)
                    {
                        var that = Marshal.GetDelegateForFunctionPointer<WndProcDelegateInternal>(data->WndProcDelegate);
                        var result = that(hwnd, msg, wParam, lParam);
                        if (result != null)
                        {
                            return result.Value;
                        }
                    }
                }

                return PInvokeSystray.DefWindowProc(hwnd, msg, wParam, lParam);
        }
    }

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

    public struct Data
    {
        public nint WndProcDelegate;
    }
    private Data _data;

    private readonly WndProcDelegateInternal _wndProc;

    public WindowMessageHandler(WndProcDelegate del)
        : this(ToInternalDelegate(del))
    {
        // no-op
    }

    internal WindowMessageHandler(WndProcDelegateInternal del)
    {
        // https://github.com/ControlzEx/ControlzEx/blob/cbb56cab39ffc78d9599208826f47eeab70455f7/src/ControlzEx/Controls/GlowWindow.cs#L94
        _wndProc = del;
        var delPtr = Marshal.GetFunctionPointerForDelegate(del);
        _data = new Data
        {
            WndProcDelegate = delPtr,
        };
    }

    // Pass this to the WNDCLASS.lpParam field in CreateWindowEx.
    public Data LpParamData()
    {
        return _data;
    }
}