using System.Diagnostics;
using System.Drawing;

namespace Systray;

/// <summary>
/// A point in physical screen coordinates. Unlike most points, this *is not*
/// scaled if your app is not DPI-aware & will *always* correspond to a physical
/// point on-screen.
/// </summary>
[DebuggerDisplay("({X}, {Y})")]
public readonly struct PhysicalPoint : IEquatable<PhysicalPoint>
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

    /// <summary>
    /// "Converts" this to a naive Point. Does not adjust for DPI in any way,
    /// simply gives you a more common type with the numbers exactly as they
    /// are.
    /// </summary>
    // TODO: support scaling point. PhysicalToLogicalPointForPerMonitorDPI
    // should help, except the hidden HWND fails as the basis for the point, so
    // it's not helpful.
    public Point ToPointNaive()
    {
        return new(X, Y);
    }

    public bool Equals(PhysicalPoint other) => X == other.X && Y == other.Y;

    public override bool Equals(object? obj) => obj is PhysicalPoint other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y);
}