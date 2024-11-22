using System.Runtime.InteropServices;
using Windows.Win32.UI.WindowsAndMessaging;

// TODO: public
internal class MenuItemInfoBuilder
{
    private static MENUITEMINFOW CreateBasic()
    {
        return new MENUITEMINFOW
        {
            cbSize = (uint)Marshal.SizeOf<MENUITEMINFOW>(),
        };
    }

    public static MENUITEMINFOW CreateSeparator()
    {
        var result = CreateBasic();
        result.fMask = MENU_ITEM_MASK.MIIM_FTYPE;
        result.fType = MENU_ITEM_TYPE.MFT_SEPARATOR;
        return result;
    }

    public static MENUITEMINFOW CreateString(string text)
    {
        unsafe
        {
            fixed (char* pText = text)
            {
                var result = CreateBasic();
                result.fMask = MENU_ITEM_MASK.MIIM_STRING;
                result.dwTypeData = pText;
                return result;
            }
        }
    }
}