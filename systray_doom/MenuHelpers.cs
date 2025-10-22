namespace systray_doom;

using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Systray;

// This is not a trustworthy class. Internal only.
internal class MenuHelpers
{
    public static TRACK_POPUP_MENU_FLAGS GetPopupAlignmentFlags()
    {
        // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-trackpopupmenuex#remarks
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