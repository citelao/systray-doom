namespace Systray.Menus;

using System.Runtime.InteropServices;
using Systray.NativeTypes;
using Windows.Win32.UI.WindowsAndMessaging;

/// <summary>
/// An opaque wrapper for MENUITEMINFOWs, to manage CsWin32's projections.
/// </summary>
public class MenuItemInfo
{
    internal readonly MENUITEMINFOW Info;

    internal MenuItemInfo(MENUITEMINFOW info)
    {
        Info = info;
    }
}

/// <summary>
/// A builder for creating MenuItemInfo instances, useful with MenuHelpers.InsertMenuItem.
/// </summary>
public class MenuItemInfoBuilder
{
    /// <summary>
    /// The command ID that will be sent when the menu item is selected.
    /// </summary>
    public uint? Id { get; set; }

    /// <summary>
    /// Whether the menu item is enabled.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Whether the menu item is the default item.
    /// </summary>
    public bool Default { get; set; } = false;

    /// <summary>
    /// Whether the menu item is checked.
    /// </summary>
    public bool Checked { get; set; } = false;

    /// <summary>
    /// Whether checked items should look like radio buttons.
    /// </summary>
    public bool IsRadio { get; set; } = false;

    /// <summary>
    /// Whether the menu item is highlighted.
    /// </summary>
    public bool Hilite { get; set; } = false;

    public enum ItemType
    {
        String,
        Separator,
    }
    public ItemType? Type { get; set; }

    /// <summary>
    /// The text to display for the menu item; use `&` to indicate the access key
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// A submenu for this menu item, if any. Does not take ownership of the submenu.
    /// </summary>
    public SafeHmenu? SubMenu { get; set; }

    /// <summary>
    /// Helper method to create a separator item
    /// </summary>
    /// <returns>N.B. this returns a MenuItemInfo directly, since separators don't support customization</returns>
    public static MenuItemInfo CreateSeparator()
    {
        return new MenuItemInfoBuilder
        {
            Type = ItemType.Separator,
        }.Build();
    }

    /// <summary>
    /// Helper method to create a simple string item
    /// </summary>
    /// <param name="text">The text to display for the menu item; use `&` to indicate the access key</param>
    /// <param name="id">The command ID that will be sent when the menu item is selected</param>
    /// <returns>A MenuItemInfoBuilder pre-populated with the given text and ID; it's safe to call Build()</returns>
    public static MenuItemInfoBuilder CreateString(string text, uint? id = null)
    {
        return new MenuItemInfoBuilder
        {
            Text = text,
            Id = id,
            Type = ItemType.String,
        };
    }

    /// <summary>
    /// Builds the current MenuItemInfo! You can pass this to MenuHelpers.InsertMenuItem.
    /// </summary>
    /// <returns>An opaque MenuItemInfo instance that can be used with MenuHelpers.InsertMenuItem</returns>
    public MenuItemInfo Build()
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

        basic.fState = 0;
        if (Enabled.HasValue)
        {
            basic.fMask |= MENU_ITEM_MASK.MIIM_STATE;
            basic.fState |= Enabled.Value ? MENU_ITEM_STATE.MFS_ENABLED : MENU_ITEM_STATE.MFS_DISABLED;
        }

        if (Default)
        {
            basic.fMask |= MENU_ITEM_MASK.MIIM_STATE;
            basic.fState |= MENU_ITEM_STATE.MFS_DEFAULT;
        }

        if (Checked)
        {
            basic.fMask |= MENU_ITEM_MASK.MIIM_STATE;
            basic.fState |= MENU_ITEM_STATE.MFS_CHECKED;
            if (IsRadio)
            {
                basic.fType |= MENU_ITEM_TYPE.MFT_RADIOCHECK;
            }
        }

        if (Hilite)
        {
            basic.fMask |= MENU_ITEM_MASK.MIIM_STATE;
            basic.fState |= MENU_ITEM_STATE.MFS_HILITE;
        }

        if (SubMenu != null)
        {
            basic.fMask |= MENU_ITEM_MASK.MIIM_SUBMENU;
            basic.hSubMenu = new HMENU(SubMenu.DangerousGetHandle());
        }

        return new MenuItemInfo(basic);
    }

    private static MENUITEMINFOW CreateBasic()
    {
        return new MENUITEMINFOW
        {
            cbSize = (uint)Marshal.SizeOf<MENUITEMINFOW>(),
        };
    }
}