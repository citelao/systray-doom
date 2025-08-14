using Systray.NativeTypes;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace Systray.UnitTests;

public class TrayIconUnitTests : IDisposable
{
    public static readonly Guid s_guid = new("0e1e23de-db34-4dac-b0ca-49424eac3bbd");
    public static readonly NoReleaseHwnd s_fakeHwnd = new(0x12345678);

    public void Dispose()
    {
        TrayIcon.Shell_NotifyIconFn = NotifyIcon.Shell_NotifyIcon;
    }

    [Fact]
    public void TestBasicPropertyValidation()
    {
        TrayIcon.Shell_NotifyIconFn = (NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            // We don't actually want to do anything here.
            return true;
        };

        uint callbackMessage = 0x0400 + 1;
        var icon = new TrayIcon(
            guid: s_guid,
            ownerHwnd: s_fakeHwnd,
            shouldHandleMessages: false,
            callbackMessage: callbackMessage);

        Assert.Equal(s_guid, icon.Guid);
        Assert.Equal(s_fakeHwnd, icon.OwnerHwnd);
        Assert.Equal(callbackMessage, icon.CallbackMessage);
        Assert.Equal("", icon.Tooltip);
        Assert.NotEqual(NoReleaseHicon.Null, icon.Icon);
    }

    [Fact]
    public void TestRegistrationFunctions()
    {
        var messages = new List<(NOTIFY_ICON_MESSAGE Message, NOTIFYICONDATAW Data)>();
        TrayIcon.Shell_NotifyIconFn = (NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            messages.Add((dwMessage, lpData));
            return true;
        };

        var icon = new TrayIcon(
            guid: s_guid,
            ownerHwnd: s_fakeHwnd,
            shouldHandleMessages: false);

        Assert.Equal(2, messages.Count);

        // First, we add the icon.
        Assert.Equal(NOTIFY_ICON_MESSAGE.NIM_ADD, messages[0].Message);
        Assert.Equal(s_fakeHwnd.AsHWND(), messages[0].Data.hWnd);
        Assert.Equal(0u, messages[0].Data.uID);
        Assert.Equal(NOTIFY_ICON_DATA_FLAGS.NIF_GUID | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP, messages[0].Data.uFlags);
        Assert.Equal(0u, messages[0].Data.uCallbackMessage);
        Assert.NotEqual(NoReleaseHicon.Null.AsHICON(), messages[0].Data.hIcon);
        Assert.Equal("", messages[0].Data.Tip.ToString());
        Assert.Equal(default, messages[0].Data.dwState);
        Assert.Equal(default, messages[0].Data.dwStateMask);
        Assert.Equal("", messages[0].Data.Info);
        Assert.Equal(PInvokeSystray.NOTIFYICON_VERSION_4, messages[0].Data.Anonymous.uVersion);
        Assert.Equal("", messages[0].Data.InfoTitle);
        Assert.Equal(NOTIFY_ICON_INFOTIP_FLAGS.NIIF_NONE, messages[0].Data.dwInfoFlags);
        Assert.Equal(s_guid, messages[0].Data.guidItem);
        Assert.Equal(NoReleaseHicon.Null.AsHICON(), messages[0].Data.hBalloonIcon);

        // Then, we set the version.
        Assert.Equal(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, messages[1].Message);
        Assert.Equal(NOTIFY_ICON_DATA_FLAGS.NIF_GUID | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP, messages[1].Data.uFlags);
        Assert.Equal(s_guid, messages[1].Data.guidItem);
        Assert.Equal(PInvokeSystray.NOTIFYICON_VERSION_4, messages[1].Data.Anonymous.uVersion);
    }

    [Fact]
    public void TestSetTooltip()
    {
        var messages = new List<(NOTIFY_ICON_MESSAGE Message, NOTIFYICONDATAW Data)>();
        TrayIcon.Shell_NotifyIconFn = (NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            messages.Add((dwMessage, lpData));
            return true;
        };

        var icon = new TrayIcon(
            guid: s_guid,
            ownerHwnd: s_fakeHwnd,
            shouldHandleMessages: false);

        Assert.Equal(2, messages.Count);

        // Change the tooltip.
        messages.Clear();
        icon.Tooltip = "New Tooltip";

        Assert.Single(messages);
        Assert.Equal(NOTIFY_ICON_MESSAGE.NIM_MODIFY, messages[0].Message);
        Assert.Equal(s_guid, messages[0].Data.guidItem);
        Assert.Equal(NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP | NOTIFY_ICON_DATA_FLAGS.NIF_GUID, messages[0].Data.uFlags);
        Assert.Equal("New Tooltip", messages[0].Data.Tip.ToString());

        Assert.Equal("New Tooltip", icon.Tooltip);
    }

    [Fact]
    public void TestSetIcon()
    {
        var messages = new List<(NOTIFY_ICON_MESSAGE Message, NOTIFYICONDATAW Data)>();
        TrayIcon.Shell_NotifyIconFn = (NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            messages.Add((dwMessage, lpData));
            return true;
        };

        var icon = new TrayIcon(
            guid: s_guid,
            ownerHwnd: s_fakeHwnd,
            shouldHandleMessages: false);

        Assert.Equal(2, messages.Count);

        // Change the icon.
        messages.Clear();
        var newIcon = new NoReleaseHicon(new IntPtr(0x87654321));
        icon.Icon = newIcon;

        Assert.Single(messages);
        Assert.Equal(NOTIFY_ICON_MESSAGE.NIM_MODIFY, messages[0].Message);
        Assert.Equal(s_guid, messages[0].Data.guidItem);
        Assert.Equal(NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP | NOTIFY_ICON_DATA_FLAGS.NIF_GUID, messages[0].Data.uFlags);
        Assert.Equal(newIcon.AsHICON(), messages[0].Data.hIcon);

        Assert.Equal(newIcon, icon.Icon);
    }
}
