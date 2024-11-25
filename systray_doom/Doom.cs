using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.UI.Shell;

// TODO: this is secretly a static singleton class. It would be nice to
// generalize it.
public class Doom
{
    private static CancellationTokenSource _cts = new CancellationTokenSource();
    private static HICON _lastIcon = HICON.Null;

    public static Task RunAsync()
    {
        return Task.Run(() => {
            unsafe
            {
                var game = PInvokeDoom.create_game(
                    DrawFrame,
                    KeyCallback,
                    SetWindowTitle
                );

                PInvokeDoom.start_game(game);
            }
        }, _cts.Token);
    }

    public static void Stop()
    {
        _cts.Cancel();
    }

    static unsafe void DrawFrame(UInt32* frame, nint xres, nint yres)
    {
        if (_cts.Token.IsCancellationRequested)
        {
            return;
        }

        // Console.WriteLine($"DrawFrame: {xres}x{yres}");
        // var desiredSizePx = (height: 200, width: 200);
        var desiredSizePx = (height: 320, width: 320);
        // var desiredSizePx = (height: 400, width: 300);
        // var desiredSizePx = (height: (int)yres, width: (int)xres);

        // Assert that the desired size is smaller than the actual size.
        Debug.Assert(desiredSizePx.width <= xres);
        Debug.Assert(desiredSizePx.height <= yres);

        var xRange = (
            min: (xres - desiredSizePx.width) / 2,
            max: (xres - desiredSizePx.width) / 2 + desiredSizePx.width
        );
        // var xRange = (
        //     min: 0,
        //     max: desiredSizePx.width
        // );
        var yRange = (
            min: 0,
            max: desiredSizePx.height
        );

        var rgbaPixelArray = new byte[desiredSizePx.width * desiredSizePx.height * 4];

        // Convert the frame pointer array to a managed array.
        var currentPixelIndex = 0;
        var totalPixels = xres * yres;
        for (var i = 0; i < totalPixels; i++)
        {
            var currentPosn = (x: i % xres, y: i / xres);
            if (currentPosn.x < xRange.min || currentPosn.x >= xRange.max)
            {
                continue;
            }
            if (currentPosn.y < yRange.min || currentPosn.y >= yRange.max)
            {
                continue;
            }
            // Console.WriteLine($"Current pixel: {i} {currentPosn.x}, {currentPosn.y}");

            var argb = frame[i];
            var r = (byte)((argb >> 16) & 0xFF);
            var g = (byte)((argb >> 8) & 0xFF);
            var b = (byte)((argb >> 0) & 0xFF);
            // Alpha seems to be opacity. Inverting it.
            var a = (byte)(255 - (argb >> 24) & 0xFF);
            rgbaPixelArray[currentPixelIndex++] = r;
            rgbaPixelArray[currentPixelIndex++] = g;
            rgbaPixelArray[currentPixelIndex++] = b;
            rgbaPixelArray[currentPixelIndex++] = a;

            // if (i == 0)
            // {
            //     Console.WriteLine($"First pixel: 0x{frame[i]:x} -> R: {rgbaPixelArray[i * 4 + 0]:x}, G: {rgbaPixelArray[i * 4 + 1]:x}, B: {rgbaPixelArray[i * 4 + 2]:x}, A: {rgbaPixelArray[i * 4 + 3]:x}");
            // }
        }

        // https://stackoverflow.com/a/537722/788168
        GCHandle pinnedArray = GCHandle.Alloc(rgbaPixelArray, GCHandleType.Pinned);
        IntPtr pointer = pinnedArray.AddrOfPinnedObject();

        var icon = PInvoke.CreateIcon(
            default(HINSTANCE),
            desiredSizePx.width,
            desiredSizePx.height,
            4,
            8,
            (byte*)pointer.ToPointer(),
            (byte*)pointer.ToPointer()
        );

        pinnedArray.Free();

        // trayIcon.Icon = icon;
        var notificationIconData = new TrayIconMessageBuilder(guid: Constants.SystrayGuid)
        {
            Icon = icon,
        }.Build();
        if (!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, notificationIconData))
        {
            throw new Exception("Failed to modify icon in the notification area.");
        }

        // Clean up the old icon.
        if (!_lastIcon.IsNull)
        {
            PInvoke.DestroyIcon(_lastIcon);
        }

        _lastIcon = icon;
    }

    static unsafe PInvokeDoom.CKeyData* KeyCallback()
    {
        if (_cts.Token.IsCancellationRequested)
        {
            return null;
        }

        // Console.WriteLine("KeyCallback");
        return null;
    }

    static unsafe void SetWindowTitle(byte* title, nint size)
    {
        if (_cts.Token.IsCancellationRequested)
        {
            return;
        }

        var titleString = System.Text.Encoding.UTF8.GetString(title, (int)size);
        Console.WriteLine($"SetWindowTitle: {titleString}");

        var notificationIconData = new TrayIconMessageBuilder(guid: Constants.SystrayGuid)
        {
            Tooltip = titleString,
        }.Build();
        if (!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, notificationIconData))
        {
            throw new Exception("Failed to modify icon in the notification area.");
        }
    }
}