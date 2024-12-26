namespace Systray;

using System.Runtime.InteropServices;

// CsWin32 loves SafeHandles params in the safe wrappers it generates (rather
// than its non-owning, direct wrappers that return HICONs [etc] directly), but
// SafeHandles are abstract.
//
// To pass null to them, or non-owning references, we must create a concrete
// implementation that does not release its value automatically.
//
// Adapted from:
// https://github.com/microsoft/CsWin32/blob/99ddd314ea359d3a97afa82c735b6a25eb25ea32/test/WinRTInteropTest/Program.cs#L144
public class NoReleaseSafeHandle : SafeHandle
{
    public static NoReleaseSafeHandle Null = new NoReleaseSafeHandle(0);

    public NoReleaseSafeHandle(int value)
        : base(IntPtr.Zero, true)
    {
        this.SetHandle(new IntPtr(value));
    }

    public override bool IsInvalid {
        get {
            return this.handle == IntPtr.Zero;
        }
    }

    protected override bool ReleaseHandle()
    {
        return true;
    }
}
