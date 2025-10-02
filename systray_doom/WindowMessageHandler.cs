namespace systray_doom;

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

internal class WindowMessageHandler
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
                        var id = data->ID;
                        if (s_handlers.TryGetValue(id, out var weakRef) && weakRef.TryGetTarget(out WindowMessageHandler? that))
                        {
                            var result = that?._wndProc(hwnd, msg, wParam, lParam);
                            if (result != null)
                            {
                                return result.Value;
                            }
                        }
                    }
                }

                return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
        }
    }

    internal delegate LRESULT? WndProcDelegate(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam);

    public struct Data
    {
        public int ID;
    }
    private Data _data;
    private readonly WndProcDelegate _wndProc;

    private static readonly Dictionary<int, WeakReference<WindowMessageHandler>> s_handlers = [];
    private static int s_nextId = 1;

    public WindowMessageHandler(WndProcDelegate deleg)
    {
        _data = new Data
        {
            ID = s_nextId++,
        };
        _wndProc = deleg;
        s_handlers[_data.ID] = new WeakReference<WindowMessageHandler>(this, trackResurrection: false);
    }

    // Pass this to the WNDCLASS.lpParam field in CreateWindowEx.
    public Data LpParamData()
    {
        return _data;
    }
}