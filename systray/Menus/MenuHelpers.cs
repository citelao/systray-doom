using Systray.NativeTypes;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Systray.Menus;

public class MenuHelpers
{
    /// <summary>
    /// Create a new, empty popup menu. You can add to this via the
    /// InsertMenuItem helpers, or you can call Win32's InsertMenuItem directly.
    /// </summary>
    /// <returns>A safe (owning) handle to the popup menu</returns>
    public static SafeHmenu CreatePopupMenu()
    {
        var menu = PInvokeSystray.CreatePopupMenu();
        return new SafeHmenu(menu);
    }

    // Internal helper for inserting menu items directly.
    internal static void InsertMenuItem(HMENU menu, uint index, MENUITEMINFOW item)
    {
        PInvokeHelpers.THROW_IF_FALSE(PInvokeSystray.InsertMenuItem(new NoReleaseSafeHandle(menu.Value), index, true, item));
    }

    /// <summary>
    /// Insert a menu item into the specified menu. You can build a MenuItemInfo
    /// with MenuItemInfoBuilder.
    /// </summary>
    /// <param name="menu">The menu to append to</param>
    /// <param name="index">The position at which to insert the menu item</param>
    /// <param name="item">The menu item to insert; typically created with MenuItemInfoBuilder's `.Build()`</param>
    public static void InsertMenuItem(SafeHmenu menu, uint index, MenuItemInfo item)
    {
        PInvokeHelpers.THROW_IF_FALSE(PInvokeSystray.InsertMenuItem(menu, index, true, item.Info));
    }

    /// <summary>
    /// Get basic flags for aligning popup menus, taking into system metrics for positioning. Also ensures right-clicks activate menu items for context menus.
    /// </summary>
    /// <param name="forRightClick">If true (default), includes TPM_RIGHTBUTTON to allow right-click activation.</param>
    /// <returns>A set of flags suitable for TrackPopupMenu & TrackPopupMenuEx; you can augment them.</returns>
    internal static TRACK_POPUP_MENU_FLAGS GetPopupAlignmentFlagsInternal(bool forRightClick = true)
    {
        // Users typically expect popup menus to support right-click.
        var flags = forRightClick ? TRACK_POPUP_MENU_FLAGS.TPM_RIGHTBUTTON : TRACK_POPUP_MENU_FLAGS.TPM_LEFTBUTTON;

        // Align the popup menu correctly with the source coordinates
        //
        // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-trackpopupmenuex#remarks
        // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfoa#:~:text=SPI_GETMENUDROPALIGNMENT
        // https://github.com/microsoft/Windows-classic-samples/blob/d338bb385b1ac47073e3540dbfa810f4dcb12ed8/Samples/Win7Samples/winui/shell/appshellintegration/NotificationIcon/NotificationIcon.cpp#L217
        if (PInvokeSystray.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_MENUDROPALIGNMENT) != 0)
        {
            flags |= TRACK_POPUP_MENU_FLAGS.TPM_RIGHTALIGN;
        }
        else
        {
            flags |= TRACK_POPUP_MENU_FLAGS.TPM_LEFTALIGN;
        }
        return flags;
    }

    /// <inheritdoc cref="GetPopupAlignmentFlagsInternal"/>
    public static uint GetPopupAlignmentFlags(bool forRightClick = true)
    {
        return (uint)GetPopupAlignmentFlagsInternal(forRightClick);
    }
}