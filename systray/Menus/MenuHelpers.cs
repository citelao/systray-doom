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
}