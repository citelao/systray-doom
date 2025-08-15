using System.Diagnostics;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Systray.NativeTypes;

/// <summary>
/// Base interface for native handle/value wrappers that don't own resources.
///
/// These public wrappers are necessary because the CsWin32-generated types are
/// `internal` and cannot be exposed publicly without exposing all the other
/// P/Invoke goo. These wrappers are effectively identical and easily convert to
/// the CsWin32 types, but they are public.
/// </summary>
/// <typeparam name="TSelf">The implementing type</typeparam>
/// <typeparam name="TRaw">The underlying raw type</typeparam>
public interface INativeWrapper<TSelf, TRaw> 
    : IEquatable<TSelf>, IComparable<TSelf>
    where TSelf : struct, INativeWrapper<TSelf, TRaw>
    where TRaw : unmanaged, IEquatable<TRaw>
{
    /// <summary>
    /// The raw underlying value.
    /// </summary>
    TRaw Value { get; }

    /// <summary>
    /// A null/empty instance of this handle type.
    /// </summary>
    static abstract TSelf Null { get; }

    /// <summary>
    /// Whether this handle represents a null/empty value.
    /// </summary>
    bool IsNull { get; }
}

/// <summary>
/// A non-owning wrapper around a Win32 HWND that doesn't automatically release
/// the handle. Build for interop with CsWin32's Windows.Win32.Foundation.HWND.
/// </summary>
[DebuggerDisplay("0x{Value:X}")]
public readonly struct NoReleaseHwnd : INativeWrapper<NoReleaseHwnd, IntPtr>
{
    public IntPtr Value { get; }

    public static NoReleaseHwnd Null => new(IntPtr.Zero);

    public bool IsNull => Value == IntPtr.Zero;

    public NoReleaseHwnd(IntPtr value)
    {
        Value = value;
    }

    internal NoReleaseHwnd(HWND hwnd)
    {
        Value = hwnd.Value;
    }

    /// <summary>
    /// Converts this handle to a CsWin32 HWND.
    /// </summary>
    internal HWND ToHwnd() => new(Value);

    public bool Equals(NoReleaseHwnd other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is NoReleaseHwnd other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public int CompareTo(NoReleaseHwnd other) => Value.ToInt64().CompareTo(other.Value.ToInt64());

    public static bool operator ==(NoReleaseHwnd left, NoReleaseHwnd right) => left.Equals(right);

    public static bool operator !=(NoReleaseHwnd left, NoReleaseHwnd right) => !left.Equals(right);

    public static bool operator <(NoReleaseHwnd left, NoReleaseHwnd right) => left.CompareTo(right) < 0;

    public static bool operator >(NoReleaseHwnd left, NoReleaseHwnd right) => left.CompareTo(right) > 0;

    public static bool operator <=(NoReleaseHwnd left, NoReleaseHwnd right) => left.CompareTo(right) <= 0;

    public static bool operator >=(NoReleaseHwnd left, NoReleaseHwnd right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"0x{Value:X}";
}

/// <summary>
/// A non-owning wrapper around a Win32 HICON that doesn't automatically release
/// the handle. Build for interop with CsWin32's Windows.Win32.Foundation.HICON.
/// </summary>
[DebuggerDisplay("0x{Value:X}")]
public readonly struct NoReleaseHicon : INativeWrapper<NoReleaseHicon, IntPtr>
{
    public IntPtr Value { get; }

    public static NoReleaseHicon Null => new(IntPtr.Zero);

    public bool IsNull => Value == IntPtr.Zero;

    public NoReleaseHicon(IntPtr value)
    {
        Value = value;
    }

    internal NoReleaseHicon(HICON hicon)
    {
        Value = hicon.Value;
    }

    /// <summary>
    /// Converts this handle to a CsWin32 HICON.
    /// </summary>
    internal HICON ToHicon() => new(Value);

    public bool Equals(NoReleaseHicon other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is NoReleaseHicon other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public int CompareTo(NoReleaseHicon other) => Value.ToInt64().CompareTo(other.Value.ToInt64());

    public static bool operator ==(NoReleaseHicon left, NoReleaseHicon right) => left.Equals(right);

    public static bool operator !=(NoReleaseHicon left, NoReleaseHicon right) => !left.Equals(right);

    public static bool operator <(NoReleaseHicon left, NoReleaseHicon right) => left.CompareTo(right) < 0;

    public static bool operator >(NoReleaseHicon left, NoReleaseHicon right) => left.CompareTo(right) > 0;

    public static bool operator <=(NoReleaseHicon left, NoReleaseHicon right) => left.CompareTo(right) <= 0;

    public static bool operator >=(NoReleaseHicon left, NoReleaseHicon right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"0x{Value:X}";
}

/// <summary>
/// A wrapper around Win32 WPARAM. Build for interop with CsWin32's
/// Windows.Win32.Foundation.WPARAM.
/// </summary>
[DebuggerDisplay("0x{Value:X}")]
public readonly struct WPARAM : INativeWrapper<WPARAM, nuint>
{
    public nuint Value { get; }

    public static WPARAM Null => new(0);

    public bool IsNull => Value == 0;

    public WPARAM(nuint value)
    {
        Value = value;
    }

    /// <summary>
    /// Converts this to a CsWin32 WPARAM.
    /// </summary>
    internal Windows.Win32.Foundation.WPARAM ToWin32() => new(Value);

    public bool Equals(WPARAM other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is WPARAM other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public int CompareTo(WPARAM other) => Value.CompareTo(other.Value);

    public static bool operator ==(WPARAM left, WPARAM right) => left.Equals(right);

    public static bool operator !=(WPARAM left, WPARAM right) => !left.Equals(right);

    public static bool operator <(WPARAM left, WPARAM right) => left.CompareTo(right) < 0;

    public static bool operator >(WPARAM left, WPARAM right) => left.CompareTo(right) > 0;

    public static bool operator <=(WPARAM left, WPARAM right) => left.CompareTo(right) <= 0;

    public static bool operator >=(WPARAM left, WPARAM right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"0x{Value:X}";
}

/// <summary>
/// A wrapper around Win32 LPARAM. Build for interop with CsWin32's
/// Windows.Win32.Foundation.LPARAM.
/// </summary>
[DebuggerDisplay("0x{Value:X}")]
public readonly struct LPARAM : INativeWrapper<LPARAM, nint>
{
    public nint Value { get; }

    public static LPARAM Null => new(0);

    public bool IsNull => Value == 0;

    public LPARAM(nint value)
    {
        Value = value;
    }

    /// <summary>
    /// Converts this to a CsWin32 LPARAM.
    /// </summary>
    internal Windows.Win32.Foundation.LPARAM ToWin32() => new(Value);

    public bool Equals(LPARAM other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is LPARAM other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public int CompareTo(LPARAM other) => Value.CompareTo(other.Value);

    public static bool operator ==(LPARAM left, LPARAM right) => left.Equals(right);

    public static bool operator !=(LPARAM left, LPARAM right) => !left.Equals(right);

    public static bool operator <(LPARAM left, LPARAM right) => left.CompareTo(right) < 0;

    public static bool operator >(LPARAM left, LPARAM right) => left.CompareTo(right) > 0;

    public static bool operator <=(LPARAM left, LPARAM right) => left.CompareTo(right) <= 0;

    public static bool operator >=(LPARAM left, LPARAM right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"0x{Value:X}";
}

/// <summary>
/// A wrapper around Win32 LRESULT. Build for interop with CsWin32's
/// Windows.Win32.Foundation.LRESULT.
/// </summary>
[DebuggerDisplay("0x{Value:X}")]
public readonly struct LRESULT : INativeWrapper<LRESULT, nint>
{
    public nint Value { get; }

    public static LRESULT Null => new(0);

    public bool IsNull => Value == 0;

    public LRESULT(nint value)
    {
        Value = value;
    }

    /// <summary>
    /// Converts this to a CsWin32 LRESULT.
    /// </summary>
    internal Windows.Win32.Foundation.LRESULT ToWin32() => new(Value);

    public bool Equals(LRESULT other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is LRESULT other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public int CompareTo(LRESULT other) => Value.CompareTo(other.Value);

    public static bool operator ==(LRESULT left, LRESULT right) => left.Equals(right);

    public static bool operator !=(LRESULT left, LRESULT right) => !left.Equals(right);

    public static bool operator <(LRESULT left, LRESULT right) => left.CompareTo(right) < 0;

    public static bool operator >(LRESULT left, LRESULT right) => left.CompareTo(right) > 0;

    public static bool operator <=(LRESULT left, LRESULT right) => left.CompareTo(right) <= 0;

    public static bool operator >=(LRESULT left, LRESULT right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"0x{Value:X}";
}