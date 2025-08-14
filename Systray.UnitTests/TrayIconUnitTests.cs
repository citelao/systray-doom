using Systray.NativeTypes;

namespace Systray.UnitTests;

public class TrayIconUnitTests : IDisposable
{
    public static readonly Guid s_guid = new("0e1e23de-db34-4dac-b0ca-49424eac3bbd");

    public void Dispose()
    {
        TrayIcon.Shell_NotifyIconFn = NotifyIcon.Shell_NotifyIcon;
    }

    [Fact]
    public void TestBasicPropertyValidation()
    {
        TrayIcon.Shell_NotifyIconFn = (Windows.Win32.UI.Shell.NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            // We don't actually want to do anything here.
            return true;
        };

        uint callbackMessage = 0x0400 + 1;
        var icon = new TrayIcon(
            guid: s_guid,
            ownerHwnd: NoReleaseHwnd.Null,
            shouldHandleMessages: false,
            callbackMessage: callbackMessage);

        Assert.Equal(s_guid, icon.Guid);
        Assert.Equal(NoReleaseHwnd.Null, icon.OwnerHwnd);
        Assert.Equal(callbackMessage, icon.CallbackMessage);
    }
}
