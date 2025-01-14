namespace systray_doom;

using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.UI.Shell;
using Systray;

// TODO: this is secretly a static singleton class. It would be nice to
// generalize it.
internal class Doom
{
    private readonly CancellationTokenSource _cts = new();
    private byte[]? _lastRgbaFrame = null;

    // Console.WriteLine($"DrawFrame: {xres}x{yres}");
    // var desiredSizePx = (height: 200, width: 200);
    // var desiredSizePx = (height: 320, width: 320);
    // var desiredSizePx = (height: 400, width: 300);
    // var desiredSizePx = (height: (int)yres, width: (int)xres);
    public static readonly (int width, int height) DesiredSizePx = (320, 320);

    // Our Doom happens to render at 640x400.
    public static readonly Size OriginalSize = new(640, 400);

    public byte[]? LastRgbaFrame
    {
        get
        {
            return _lastRgbaFrame;
        }
    }

    public class FrameDrawnEventArgs
    {
        public required byte[] RgbaFrame;
        public required byte[] BgraFrame;

        public required Size FullSize;
        public required byte[] FullRgbaFrame;
    }
    public event Action<FrameDrawnEventArgs>? FrameDrawn;
    public event Action<string>? TitleChanged;

    public Doom()
    {
    }

    public Task RunAsync()
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

    public void Stop()
    {
        Console.WriteLine("Stopping Doom");
        _cts.Cancel();
    }

    unsafe void DrawFrame(UInt32* frame, nint xres, nint yres)
    {
        try
        {
            DrawFrameInternal(frame, xres, yres);
        }
        catch (Exception e)
        {
            // Best-effort. Any exceptions here will disappear.
            //
            // Particularly: setting TrayIcon.Icon will throw if explorer has
            // crashed (e.g. there's no icon).
            Console.Error.WriteLine($"Error in DrawFrame: {e}");
        }
    }

    unsafe void DrawFrameInternal(UInt32* frame, nint xres, nint yres)
    {
        if (_cts.Token.IsCancellationRequested)
        {
            return;
        }

        var desiredSizePx = DesiredSizePx;

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
        var bgraPixelArray = new byte[desiredSizePx.width * desiredSizePx.height * 4];

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

            bgraPixelArray[currentPixelIndex - 4] = b;
            bgraPixelArray[currentPixelIndex - 3] = g;
            bgraPixelArray[currentPixelIndex - 2] = r;
            bgraPixelArray[currentPixelIndex - 1] = a;

            // if (i == 0)
            // {
            //     Console.WriteLine($"First pixel: 0x{frame[i]:x} -> R: {rgbaPixelArray[i * 4 + 0]:x}, G: {rgbaPixelArray[i * 4 + 1]:x}, B: {rgbaPixelArray[i * 4 + 2]:x}, A: {rgbaPixelArray[i * 4 + 3]:x}");
            // }
        }

        var fullRgbaPixelArray = new byte[xres * yres * 4];
        for (var i = 0; i < totalPixels; i++)
        {
            var argb = frame[i];
            var r = (byte)((argb >> 16) & 0xFF);
            var g = (byte)((argb >> 8) & 0xFF);
            var b = (byte)((argb >> 0) & 0xFF);
            // Alpha seems to be opacity. Inverting it.
            var a = (byte)(255 - (argb >> 24) & 0xFF);
            fullRgbaPixelArray[i * 4 + 0] = r;
            fullRgbaPixelArray[i * 4 + 1] = g;
            fullRgbaPixelArray[i * 4 + 2] = b;
            fullRgbaPixelArray[i * 4 + 3] = a;
        }

        // Fire the FrameDrawn event
        FrameDrawn?.Invoke(new FrameDrawnEventArgs
        {
            RgbaFrame = rgbaPixelArray,
            BgraFrame = bgraPixelArray,

            FullSize = OriginalSize,
            FullRgbaFrame = fullRgbaPixelArray
        });

        _lastRgbaFrame = rgbaPixelArray;
    }

    unsafe PInvokeDoom.CKeyData* KeyCallback()
    {
        if (_cts.Token.IsCancellationRequested)
        {
            return null;
        }

        // Console.WriteLine("KeyCallback");
        return null;
    }

    unsafe void SetWindowTitle(byte* title, nint size)
    {
        try
        {
            SetWindowTitleInternal(title, size);
        }
        catch (Exception e)
        {
            // Best-effort. Any exceptions here will disappear.
            Console.Error.WriteLine($"Error in SetWindowTitle: {e}");
        }
    }

    unsafe void SetWindowTitleInternal(byte* title, nint size)
    {
        if (_cts.Token.IsCancellationRequested)
        {
            return;
        }

        var titleString = System.Text.Encoding.UTF8.GetString(title, (int)size);
        Console.WriteLine($"SetWindowTitle: {titleString}");

        TitleChanged?.Invoke(titleString);
    }
}