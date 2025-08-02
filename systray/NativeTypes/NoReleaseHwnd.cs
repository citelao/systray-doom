using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Systray.NativeTypes;

// TODO: generalize into TypedIntPtr?
// TODO: can we simply make a subset of PInvoke types public?
public struct NoReleaseHwnd
{
    internal readonly IntPtr Value;

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

public struct Lparam
{
    public readonly nint Value;

    public Lparam(nint value)
    {
        Value = value;
    }

    internal LPARAM AsLPARAM() => new LPARAM(Value);
}