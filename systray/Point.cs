using System.Diagnostics;
using System.Drawing;
using Systray.NativeTypes;
using Windows.Win32;

namespace Systray;

/// <summary>
/// A point in physical screen coordinates. Unlike most points, this *is not*
/// scaled if your app is not DPI-aware & will *always* correspond to a physical
/// point on-screen.
/// </summary>
// TODO: use source generators?
[DebuggerDisplay("({X}, {Y})")]
public readonly struct PhysicalPoint
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

    // TODO: support scaling point. PhysicalToLogicalPointForPerMonitorDPI
    // should help, except the hidden HWND fails as the basis for the point, so
    // it's not helpful.
    public Point ToPoint()
    {
        return new(X, Y);
    }
}