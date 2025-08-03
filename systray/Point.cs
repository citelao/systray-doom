using System.Diagnostics;
using System.Drawing;
using Systray.NativeTypes;
using Windows.Win32;

namespace Systray;

// A point in physical screen coordinates. Unlike most points, this *is not*
// scaled if your app is not DPI-aware & will *always* correspond to a physical
// point on-screen.
//
// TODO: use source generators?
[DebuggerDisplay("({X}, {Y})")]
public struct PhysicalPoint
{
    public readonly int X;
    public readonly int Y;

    public PhysicalPoint(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }

    public Point ToPoint()
    {
        return new(X, Y);
    }

    // Convert this point to the client space of a window. If the window is not
    // per-monitor DPI-aware, the point will be scaled to the window's DPI.
    // Since per-monitor DPI-aware apps always use physical pixels, if the
    // window *is* per-monitor DPI-aware, the point will simply be moved into
    // the client space, no scaling necessary.
    public Point ToClientCoordinate(NoReleaseHwnd hwnd)
    {
        var result = PInvokeHelpers.PhysicalToLogicalPointForPerMonitorDPI(hwnd.AsHWND(), ToPoint());
        return result;
    }

    // Convert this point into the "screen coordinate" space that a given window
    // sees.
    //
    // For per-monitor DPI-aware apps, this will no-op (since screen coordinate
    // space *is* physical coordinate space). For non-aware apps, this will
    // scale the point into the scaled screen coordinate space the window sees.
    public Point ToScaledScreenCoordinate(NoReleaseHwnd hwnd)
    {
        var client = ToClientCoordinate(hwnd);
        var screen = PInvokeHelpers.ClientToScreen(hwnd.AsHWND(), client);
        return screen;
    }
}