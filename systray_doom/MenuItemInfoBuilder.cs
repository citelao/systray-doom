using System.Runtime.InteropServices;
using Windows.Win32.UI.WindowsAndMessaging;

// TODO: public
internal class MenuItemInfoBuilder
{
    public uint? Id { get; set; }
    public bool? Enabled { get; set; }

    public enum ItemType
    {
        String,
        Separator,
    }
    public ItemType? Type { get; set; }
    public string? Text { get; set; }

    public static MENUITEMINFOW CreateSeparator()
    {
        return new MenuItemInfoBuilder
        {
            Type = ItemType.Separator,
        }.Build();
    }

    public static MenuItemInfoBuilder CreateString(string text, uint? id = null)
    {
        return new MenuItemInfoBuilder
        {
            Text = text,
            Id = id,
            Type = ItemType.String,
        };
    }

    public MENUITEMINFOW Build()
    {
        var basic = CreateBasic();
        if (Text != null)
        {
            if ((Type ?? ItemType.String) != ItemType.String)
            {
                throw new InvalidOperationException("Text is only valid for string items.");
            }

            unsafe
            {
                fixed (char* pText = Text)
                {
                    basic.fMask |= MENU_ITEM_MASK.MIIM_STRING;
                    basic.dwTypeData = pText;
                }
            }
        }

        switch (Type)
        {
            case null:
                if (Text == null)
                {
                    throw new InvalidOperationException("Text or type is required");
                }
                break;

            case ItemType.String:
                // Handled above.
                // basic.fMask |= MENU_ITEM_MASK.MIIM_FTYPE;
                // basic.fType = MENU_ITEM_TYPE.MFT_STRING;
                break;
            case ItemType.Separator:
                basic.fMask |= MENU_ITEM_MASK.MIIM_FTYPE;
                basic.fType = MENU_ITEM_TYPE.MFT_SEPARATOR;
                break;
            default:
                throw new InvalidOperationException($"Unknown item type: {Type}");
        }

        if (Id.HasValue)
        {
            basic.fMask |= MENU_ITEM_MASK.MIIM_ID;
            basic.wID = Id.Value;
        }

        if (Enabled.HasValue)
        {
            basic.fMask |= MENU_ITEM_MASK.MIIM_STATE;
            basic.fState = Enabled.Value ? MENU_ITEM_STATE.MFS_ENABLED : MENU_ITEM_STATE.MFS_DISABLED;
        }

        return basic;
    }

    private static MENUITEMINFOW CreateBasic()
    {
        return new MENUITEMINFOW
        {
            cbSize = (uint)Marshal.SizeOf<MENUITEMINFOW>(),
        };
    }
}