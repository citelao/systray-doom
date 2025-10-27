using System.Runtime.InteropServices;
using Systray.Menus;
using Systray.NativeTypes;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Systray.UnitTests;

public class MenuItemInfoUnitTests
{
    /// <summary>
    /// Helper method to check if a specific mask flag is enabled in MENUITEMINFOW.fMask
    /// </summary>
    private static bool IsMaskFlagEnabled(MENUITEMINFOW menuInfo, MENU_ITEM_MASK flag)
    {
        return (menuInfo.fMask & flag) != 0;
    }

    /// <summary>
    /// Helper method to check if a specific state flag is enabled in MENUITEMINFOW.fState
    /// </summary>
    private static bool IsStateFlagEnabled(MENUITEMINFOW menuInfo, MENU_ITEM_STATE flag)
    {
        return (menuInfo.fState & flag) != 0;
    }

    [Fact]
    public void TestCreateSeparator()
    {
        var separator = MenuItemInfo.CreateSeparator();
        
        // Verify managed object state
        Assert.Equal(MenuItemInfo.ItemType.Separator, separator.Type);
        Assert.Null(separator.Text);
        Assert.Null(separator.Id);
        Assert.Null(separator.Enabled);
        Assert.False(separator.Default);
        Assert.False(separator.Checked);
        Assert.False(separator.Hilite);
        Assert.Null(separator.SubMenu);

        // Verify native structure
        unsafe
        {
            var menuInfo = separator.BuildNative(null);

            Assert.Equal((uint)Marshal.SizeOf<MENUITEMINFOW>(), menuInfo.cbSize);
            Assert.True(IsMaskFlagEnabled(menuInfo, MENU_ITEM_MASK.MIIM_FTYPE));
            Assert.Equal(MENU_ITEM_TYPE.MFT_SEPARATOR, menuInfo.fType);
            Assert.False(IsMaskFlagEnabled(menuInfo, MENU_ITEM_MASK.MIIM_STRING));
            Assert.Equal(IntPtr.Zero, new IntPtr(menuInfo.dwTypeData));
        }
    }

    [Fact]
    public void TestCreateString()
    {
        var item = MenuItemInfo.CreateString("Test Item", 42);
        
        // Verify managed object state
        Assert.Equal(MenuItemInfo.ItemType.String, item.Type);
        Assert.Equal("Test Item", item.Text);
        Assert.Equal(42u, item.Id);
        Assert.Null(item.Enabled);
        Assert.False(item.Default);
        Assert.False(item.Checked);
        Assert.False(item.Hilite);
        Assert.Null(item.SubMenu);

        // Verify native structure
        unsafe
        {
            fixed (char* pText = item.Text)
            {
                var menuInfo = item.BuildNative(pText);

                Assert.Equal((uint)Marshal.SizeOf<MENUITEMINFOW>(), menuInfo.cbSize);
                Assert.True(IsMaskFlagEnabled(menuInfo, MENU_ITEM_MASK.MIIM_ID));
                Assert.Equal(42u, menuInfo.wID);
                Assert.True(IsMaskFlagEnabled(menuInfo, MENU_ITEM_MASK.MIIM_STRING));
                Assert.Equal(new IntPtr(pText), new IntPtr(menuInfo.dwTypeData));
                // No state flags should be set since Enabled is null
                Assert.Equal(0u, (uint)menuInfo.fState);
            }
        }
    }

    [Fact]
    public void TestCreateStringWithoutId()
    {
        var item = MenuItemInfo.CreateString("Test Item");
        
        // Verify managed object state
        Assert.Equal(MenuItemInfo.ItemType.String, item.Type);
        Assert.Equal("Test Item", item.Text);
        Assert.Null(item.Id);

        // Verify native structure
        unsafe
        {
            fixed (char* pText = item.Text)
            {
                var menuInfo = item.BuildNative(pText);

                Assert.Equal((uint)Marshal.SizeOf<MENUITEMINFOW>(), menuInfo.cbSize);
                Assert.False(IsMaskFlagEnabled(menuInfo, MENU_ITEM_MASK.MIIM_ID));
                Assert.True(IsMaskFlagEnabled(menuInfo, MENU_ITEM_MASK.MIIM_STRING));
                Assert.Equal(new IntPtr(pText), new IntPtr(menuInfo.dwTypeData));
            }
        }
    }

    [Fact]
    public void TestStringItemWithProperties()
    {
        var item = MenuItemInfo.CreateString("Test &Item", 42);
        item.Enabled = true;
        item.Default = true;

        // Verify managed object state
        Assert.NotNull(item.Text);
        Assert.Equal("Test &Item", item.Text);
        Assert.Equal(42u, item.Id);
        Assert.True(item.Enabled);
        Assert.True(item.Default);

        // Verify native structure
        unsafe
        {
            fixed (char* pText = item.Text)
            {
                var menuInfo = item.BuildNative(pText);

                Assert.Equal((uint)Marshal.SizeOf<MENUITEMINFOW>(), menuInfo.cbSize);
                Assert.True(IsMaskFlagEnabled(menuInfo, MENU_ITEM_MASK.MIIM_ID));
                Assert.Equal(42u, menuInfo.wID);
                Assert.True(IsMaskFlagEnabled(menuInfo, MENU_ITEM_MASK.MIIM_STRING));
                Assert.Equal(new IntPtr(pText), new IntPtr(menuInfo.dwTypeData));
                Assert.True(IsMaskFlagEnabled(menuInfo, MENU_ITEM_MASK.MIIM_STATE));
                Assert.False(IsStateFlagEnabled(menuInfo, MENU_ITEM_STATE.MFS_DISABLED));
                Assert.True(IsStateFlagEnabled(menuInfo, MENU_ITEM_STATE.MFS_DEFAULT));
            }
        }
    }

    [Fact]
    public void TestEmptyStringText()
    {
        var item = MenuItemInfo.CreateString("", 789);
        
        // Verify managed object state
        Assert.Equal("", item.Text);
        Assert.Equal(MenuItemInfo.ItemType.String, item.Type);

        // Verify native structure
        unsafe
        {
            fixed (char* pText = item.Text)
            {
                var menuInfo = item.BuildNative(pText);

                Assert.True(IsMaskFlagEnabled(menuInfo, MENU_ITEM_MASK.MIIM_STRING));
                Assert.Equal(new IntPtr(pText), new IntPtr(menuInfo.dwTypeData));
                Assert.Equal(789u, menuInfo.wID);
            }
        }
    }

    [Fact]
    public void TestUnicodeText()
    {
        var item = MenuItemInfo.CreateString("Test 测试 🚀 Item", 123);
        
        // Verify managed object state
        Assert.Equal("Test 测试 🚀 Item", item.Text);
        Assert.Equal(123u, item.Id);

        // Verify native structure
        unsafe
        {
            fixed (char* pText = item.Text)
            {
                var menuInfo = item.BuildNative(pText);

                Assert.True(IsMaskFlagEnabled(menuInfo, MENU_ITEM_MASK.MIIM_STRING));
                Assert.Equal(new IntPtr(pText), new IntPtr(menuInfo.dwTypeData));
                
                // Verify the text pointer points to the correct Unicode string
                var actualText = Marshal.PtrToStringUni(new IntPtr(menuInfo.dwTypeData));
                Assert.Equal("Test 测试 🚀 Item", actualText);
            }
        }
    }

    [Fact]
    public void TestTextValidationForSeparator()
    {
        var separator = MenuItemInfo.CreateSeparator();
        separator.Text = "Should not work";

        // This should throw when trying to build native structure
        // because separators shouldn't have text
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            unsafe
            {
                fixed (char* pText = separator.Text)
                {
                    // Access the internal BuildNative method directly
                    separator.BuildNative(pText);
                }
            }
        });
        Assert.Contains("Text is only valid for string items", ex.Message);
    }

    [Fact]
    public void TestValidationRequiresTextOrType()
    {
        var item = new MenuItemInfo(); // No text or type set

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            unsafe
            {
                item.BuildNative(null);
            }
        });
        Assert.Contains("Text or type is required", ex.Message);
    }

    [Fact]
    public void TestAllProperties()
    {
        var item = new MenuItemInfo
        {
            Id = 999,
            Enabled = false,
            Default = true,
            Checked = true,
            Hilite = true,
            Type = MenuItemInfo.ItemType.String,
            Text = "Full Test Item",
            // SubMenu would require a real SafeHmenu, so we'll skip that for this test
        };

        // Verify managed object state
        Assert.Equal(999u, item.Id);
        Assert.False(item.Enabled);
        Assert.True(item.Default);
        Assert.True(item.Checked);
        Assert.True(item.Hilite);
        Assert.Equal(MenuItemInfo.ItemType.String, item.Type);
        Assert.Equal("Full Test Item", item.Text);

        // Verify native structure
        unsafe
        {
            fixed (char* pText = item.Text)
            {
                var menuInfo = item.BuildNative(pText);

                Assert.Equal((uint)Marshal.SizeOf<MENUITEMINFOW>(), menuInfo.cbSize);
                Assert.True(IsMaskFlagEnabled(menuInfo, MENU_ITEM_MASK.MIIM_ID));
                Assert.Equal(999u, menuInfo.wID);
                Assert.True(IsMaskFlagEnabled(menuInfo, MENU_ITEM_MASK.MIIM_STRING));
                Assert.Equal(new IntPtr(pText), new IntPtr(menuInfo.dwTypeData));
                Assert.True(IsMaskFlagEnabled(menuInfo, MENU_ITEM_MASK.MIIM_STATE));
                Assert.True(IsStateFlagEnabled(menuInfo, MENU_ITEM_STATE.MFS_DISABLED));
                Assert.True(IsStateFlagEnabled(menuInfo, MENU_ITEM_STATE.MFS_DEFAULT));
                Assert.True(IsStateFlagEnabled(menuInfo, MENU_ITEM_STATE.MFS_CHECKED));
                Assert.True(IsStateFlagEnabled(menuInfo, MENU_ITEM_STATE.MFS_HILITE));
            }
        }
    }

    [Fact]
    public void TestDisabledItem()
    {
        var item = MenuItemInfo.CreateString("Disabled Item", 123);
        item.Enabled = false;

        // Verify managed object state
        Assert.False(item.Enabled);

        // Verify native structure
        unsafe
        {
            fixed (char* pText = item.Text)
            {
                var menuInfo = item.BuildNative(pText);

                Assert.True(IsMaskFlagEnabled(menuInfo, MENU_ITEM_MASK.MIIM_STATE));
                Assert.True(IsStateFlagEnabled(menuInfo, MENU_ITEM_STATE.MFS_DISABLED));
            }
        }
    }

    [Fact]
    public void TestNullEnabledProperty()
    {
        var item = MenuItemInfo.CreateString("Item", 456);
        // Don't set Enabled property (leave as null)

        // Verify managed object state
        Assert.Null(item.Enabled);

        // Verify native structure
        unsafe
        {
            fixed (char* pText = item.Text)
            {
                var menuInfo = item.BuildNative(pText);

                // Verify no state flag is set for enabled/disabled when Enabled is null
                Assert.Equal(0u, (uint)menuInfo.fState);
            }
        }
    }

    [Fact]
    public void TestEnabledVsDisabled_FlagValueDocumentation()
    {
        // This test documents the actual flag values for reference
        var enabledItem = MenuItemInfo.CreateString("Enabled", 1);
        enabledItem.Enabled = true;
        
        var disabledItem = MenuItemInfo.CreateString("Disabled", 2);
        disabledItem.Enabled = false;

        // Verify managed object states
        Assert.True(enabledItem.Enabled);
        Assert.False(disabledItem.Enabled);

        // Verify native structures and document flag behavior
        unsafe
        {
            fixed (char* pEnabled = enabledItem.Text)
            fixed (char* pDisabled = disabledItem.Text)
            {
                var enabledInfo = enabledItem.BuildNative(pEnabled);
                var disabledInfo = disabledItem.BuildNative(pDisabled);

                // Document the values for debugging
                var enabledState = (uint)enabledInfo.fState;
                var disabledState = (uint)disabledInfo.fState;

                // The key assertion: enabled and disabled items should have different states
                Assert.NotEqual(enabledState, disabledState);
                
                // Disabled items should have the MFS_DISABLED flag
                Assert.True(IsStateFlagEnabled(disabledInfo, MENU_ITEM_STATE.MFS_DISABLED));
                
                // This test helps us understand that MFS_ENABLED might be 0
                // In Windows, MFS_ENABLED is typically 0x00000000 and MFS_DISABLED is 0x00000003
            }
        }
    }
}