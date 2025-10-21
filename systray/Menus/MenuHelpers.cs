using Systray.NativeTypes;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Systray.Menus;

public class MenuHelpers
{
    /// <summary>
    /// Create a new, empty popup menu.
    /// </summary>
    /// <returns>A safe (owning) handle to the popup menu</returns>
    public static SafeHmenu CreatePopupMenu()
    {
        var menu = PInvokeSystray.CreatePopupMenu();
        return new SafeHmenu(menu);
    }

    internal static void InsertMenuItem(HMENU menu, uint index, MENUITEMINFOW item)
    {
        PInvokeHelpers.THROW_IF_FALSE(PInvokeSystray.InsertMenuItem(new NoReleaseSafeHandle((int)menu.Value), index, true, item));
    }

    // TODO: object
    public static void InsertMenuItem(SafeHmenu menu, uint index, object item)
    {
        var itemInfo = (MENUITEMINFOW)item;
        PInvokeHelpers.THROW_IF_FALSE(PInvokeSystray.InsertMenuItem(menu, index, true, itemInfo));
    }
}