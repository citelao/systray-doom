using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Systray.NativeTypes;

/// <summary>
/// A SafeHandle wrapper around a Win32 HMENU that automatically calls DestroyMenu
/// when the handle is disposed or finalized.
/// </summary>
public sealed class SafeHmenu : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeHmenu() : base(ownsHandle: true)
    {
    }

    internal SafeHmenu(HMENU hmenu) : base(ownsHandle: true)
    {
        SetHandle(hmenu.Value);
    }

    internal HMENU DangerousToHMENU()
    {
        return new HMENU(handle);
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            return PInvokeSystray.DestroyMenu(DangerousToHMENU());
        }
        return true;
    }
}
