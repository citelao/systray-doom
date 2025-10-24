using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Systray;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

internal class MessageOnlyWindow : IDisposable
{
    internal delegate LRESULT? WndProcDelegate(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam);

    private static readonly Dictionary<int, WeakReference<MessageOnlyWindow>> s_handlers = [];
    private static int s_nextId = 1;
    public struct Data
    {
        public int ID;
    }

    internal readonly HWND Hwnd;

    private Data _data;
    private readonly WndProcDelegate _wndProc;

    unsafe public MessageOnlyWindow(string name, WndProcDelegate wndProc)
    {
        _wndProc = wndProc;
        _data = new Data
        {
            ID = s_nextId++,
        };
        s_handlers[_data.ID] = new WeakReference<MessageOnlyWindow>(this);

        fixed (char* fixedName = name)
        {
            var wndClass = new WNDCLASSEXW
            {
                cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<WNDCLASSEXW>(),
                lpfnWndProc = &StaticWndProc,
                hInstance = PInvoke.GetModuleHandle(default(PCWSTR)),
                lpszClassName = fixedName,
            };

            // We ignore the returned class atom & use the class name directly.
            // https://devblogs.microsoft.com/oldnewthing/20080501-00/?p=22503
            var atom = PInvoke.RegisterClassEx(wndClass);
            PInvokeHelpers.THROW_LAST_ERROR_IF(atom == 0, "Failed to register window class");
        }

        // https://stackoverflow.com/questions/4081334/using-createwindowex-to-make-a-message-only-window
        // https://pinvoke.net/default.aspx/Constants/HWND_MESSAGE.html
        var HWND_MESSAGE = new HWND(unchecked((nint)(-3)));
        Hwnd = PInvoke.CreateWindowEx(
            0,
            lpClassName: name,
            lpWindowName: name,
            0,
            0,
            0,
            0,
            0,
            HWND_MESSAGE,
            default,
            default,
            null);
        PInvokeHelpers.THROW_LAST_ERROR_IF(Hwnd == HWND.Null, "Failed to create window");
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static LRESULT StaticWndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case PInvoke.WM_NCCREATE:
                unsafe
                {
                    var createStruct = (CREATESTRUCTW*)lParam.Value;
                    var data = (Data*)createStruct->lpCreateParams;
                    Systray.NativeTypes.PInvokeCore.SetWindowLongPtr(new Systray.NativeTypes.NoReleaseHwnd(hwnd.Value), (int)WINDOW_LONG_PTR_INDEX.GWLP_USERDATA, (nint)data);
                }
                return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);

            default:
                unsafe
                {
                    var data = (Data*)Systray.NativeTypes.PInvokeCore.GetWindowLongPtr(new Systray.NativeTypes.NoReleaseHwnd(hwnd.Value), (int)WINDOW_LONG_PTR_INDEX.GWLP_USERDATA);
                    if (data != null)
                    {
                        var id = data->ID;
                        if (s_handlers.TryGetValue(id, out var weakRef) && weakRef.TryGetTarget(out MessageOnlyWindow? that))
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

    // TODO: message loop

    public void Dispose()
    {
        // PInvoke.DestroyWindow(Hwnd);
    }
}