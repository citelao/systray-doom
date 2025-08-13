using Systray;

namespace Systray.UnitTests;

public class TrayIconUnitTests
{
    public static readonly Guid s_guid = new("0e1e23de-db34-4dac-b0ca-49424eac3bbd");

    [Fact]
    public void TestBasicPropertyValidation()
    {
        var icon = new Systray.TrayIcon(guid: s_guid, ownerHwnd: Systray.NoReleaseHwnd.Null, shouldHandleMessages: false, callbackMessage: 0x0400 + 1);
    }
}
