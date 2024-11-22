using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

// This is not a trustworthy class.
internal class MenuHelpers
{
    public static void InsertMenuItem(HMENU menu, uint index, string text)
    {
        unsafe {
            fixed (char* pText = "Systray Doom!")
            {
                // TODO: MENUITEMINFOW builder.
                PInvokeHelpers.THROW_IF_FALSE(PInvoke.InsertMenuItem(new NoReleaseSafeHandle((int)menu.Value), index, true, new MENUITEMINFOW
                {
                    cbSize = (uint)Marshal.SizeOf<MENUITEMINFOW>(),
                    fMask = MENU_ITEM_MASK.MIIM_STRING,
                    dwTypeData = pText,
                }));
            }
        }
    }

    public static TRACK_POPUP_MENU_FLAGS GetPopupFlags()
    {
        // https://github.com/microsoft/Windows-classic-samples/blob/d338bb385b1ac47073e3540dbfa810f4dcb12ed8/Samples/Win7Samples/winui/shell/appshellintegration/NotificationIcon/NotificationIcon.cpp#L217
        var flags = TRACK_POPUP_MENU_FLAGS.TPM_RIGHTBUTTON;
        if (PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_MENUDROPALIGNMENT) != 0)
        {
            flags |= TRACK_POPUP_MENU_FLAGS.TPM_RIGHTALIGN;
        }
        else
        {
            flags |= TRACK_POPUP_MENU_FLAGS.TPM_LEFTALIGN;
        }
        return flags;
    }
}