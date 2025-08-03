using System.Diagnostics;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Systray.NativeTypes;

// // TODO: generalize into TypedIntPtr?
// // TODO: can we simply make a subset of PInvoke types public?
// [DebuggerDisplay("{Value}")]
// public readonly partial struct NativeType<RawType>
//     : IEquatable<NativeType<RawType>>
//     where RawType : IEquatable<RawType>
// {
//     public readonly RawType Value;

//     public NativeType(RawType value) => this.Value = value;

//     public static NativeType<RawType> Null => default;

//     // public bool IsNull => Value == default;
//     public bool IsNull => this == Null;

//     public static implicit operator RawType(NativeType<RawType> value) => value.Value;

//     public static explicit operator NativeType<RawType>(RawType value) => new NativeType<RawType>(value);

//     public static bool operator ==(NativeType<RawType> left, NativeType<RawType> right) => left.Value == right.Value;

//     public static bool operator !=(NativeType<RawType> left, NativeType<RawType> right) => !(left == right);

//     public bool Equals(NativeType<RawType> other) => this.Value == other.Value;

//     public override bool Equals(object obj) => obj is NativeType<RawType> other && this.Equals(other);

//     public override int GetHashCode() => this.Value.GetHashCode();

//     public override string ToString() => $"0x{this.Value:x}";

//     // public static implicit operator HANDLE(NativeType<RawType> value) => new HANDLE(value.Value);
// }

public struct NoReleaseHwnd
{
    public readonly IntPtr Value;

    public static NoReleaseHwnd Null = new(IntPtr.Zero);

    public NoReleaseHwnd(IntPtr value)
    {
        Value = value;
    }

    internal NoReleaseHwnd(HWND hwnd)
    {
        Value = hwnd.Value;
    }

    public override string ToString() => $"0x{this.Value:x}";

    // Can't be internal?
    // internal static implicit operator HWND(NoReleaseHwnd value) => new HWND(value.Value);
    internal HWND AsHWND() => new HWND(Value);
}

public struct NoReleaseHicon : IEquatable<NoReleaseHicon>
{
    internal readonly IntPtr Value;

    public static NoReleaseHicon Null = new(IntPtr.Zero);

    public NoReleaseHicon(IntPtr value)
    {
        Value = value;
    }

    internal NoReleaseHicon(HWND hwnd)
    {
        Value = hwnd.Value;
    }

    public override string ToString() => $"0x{this.Value:x}";

    // Can't be internal?
    // internal static implicit operator HWND(NoReleaseHicon value) => new HWND(value.Value);
    internal HICON AsHICON() => new HICON(Value);

    // TODO: generalize
    public bool Equals(NoReleaseHicon other)
    {
        return other.Value == this.Value;
    }
    public static bool operator ==(NoReleaseHicon left, NoReleaseHicon right) => left.Value == right.Value;
    public static bool operator !=(NoReleaseHicon left, NoReleaseHicon right) => !(left == right);
}

public struct Wparam
{
    public readonly nuint Value;

    public Wparam(nuint value)
    {
        Value = value;
    }

    internal WPARAM AsWPARAM() => new WPARAM(Value);
}

public struct Lresult
{
    public readonly nint Value;

    public Lresult(nint value)
    {
        Value = value;
    }

    internal LRESULT AsLRESULT() => new LRESULT(Value);
}

public struct Lparam
{
    public readonly nint Value;

    public Lparam(nint value)
    {
        Value = value;
    }

    internal LPARAM AsLPARAM() => new LPARAM(Value);
}