using Systray;
using Systray.Menus;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.Extensions.Logging;

// Create a simple console logger:
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "hh:mm:ss ";
    })
    // .SetMinimumLevel(LogLevel.Trace);
    .SetMinimumLevel(LogLevel.Information);
});
var logger = loggerFactory.CreateLogger<Program>();

logger.LogInformation("Starting...");

using var messageWindow = new MessageOnlyWindow("Systray.Sample.Window", (hwnd, msg, wParam, lParam) =>
{
    logger.LogInformation("Window Message: {Msg} (wParam=0x{WParam:X}, lParam=0x{LParam:X})", msg, wParam.Value, lParam.Value);
    switch (msg)
    {
        case PInvoke.WM_CLOSE:
            PInvoke.DestroyWindow(hwnd);
            return new LRESULT(0);

        case PInvoke.WM_DESTROY:
            PInvoke.PostQuitMessage(0);
            return new LRESULT(0);

        default:
            return null;
    }
});

var guid = Guid.Parse("0ec911bf-c185-400b-816e-a51689aebbfb");
var icon = new TrayIcon(guid, new(messageWindow.Hwnd.Value), logger: loggerFactory.CreateLogger<TrayIcon>());
icon.ContextMenu = (hwnd, pt) =>
{
    logger.LogInformation("Showing context menu at {X},{Y}", pt.X, pt.Y);

    using var menu = MenuHelpers.CreatePopupMenu();

    uint exitId = 1;
    MenuHelpers.InsertMenuItem(menu, 1, new MenuItemInfoBuilder { Text = "My app", Enabled = false }.Build());
    MenuHelpers.InsertMenuItem(menu, 2, MenuItemInfoBuilder.CreateSeparator());
    MenuHelpers.InsertMenuItem(menu, 3, new MenuItemInfoBuilder { Text = "E&xit", Id = exitId, Default = true }.Build());

    // Note: add TPM_LAYOUTRTL for RTL layouts.
    // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-trackpopupmenuex#:~:text=existing%20menu%20item.-,%5Bin%5D%20uFlags,-Type%3A%20UINT
    var flags = (TRACK_POPUP_MENU_FLAGS)MenuHelpers.GetPopupAlignmentFlags();
    var returnValueFlags = TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD | TRACK_POPUP_MENU_FLAGS.TPM_NONOTIFY;
    flags |= returnValueFlags;

    var response = PInvoke.TrackPopupMenuEx(
        menu,
        (uint)flags,
        pt.X,
        pt.Y,
        new HWND(hwnd.Value),
        null);
    if (response == 0)
    {
        // Either nothing selected or an error occurred.
    }
    else if (response == exitId)
    {
        // Exit!
        PInvoke.PostMessage(new HWND(hwnd.Value), PInvoke.WM_CLOSE, 0, 0);
    }

    return true;
};

// Wait for enter key
// logger.LogInformation("Press Enter to exit...");
// Console.ReadLine();

logger.LogInformation("Starting message loop...");
while (PInvoke.GetMessage(out var msg, HWND.Null, 0, 0))
{
    PInvoke.TranslateMessage(msg);
    PInvoke.DispatchMessage(msg);
}