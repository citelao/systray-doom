using Systray;
using Systray.Menus;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.Extensions.Logging;

// Create a simple console logger
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

// Create a parent message-only window to handle events.
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

// Create the tray icon!
var guid = Guid.Parse("0ec911bf-c185-400b-816e-a51689aebbfb");
var icon = new TrayIcon(guid, new(messageWindow.Hwnd.Value), logger: loggerFactory.CreateLogger<TrayIcon>());
icon.ContextMenu = (hwnd, pt) =>
{
    ShowContextMenu(new HWND(hwnd.Value), pt, isRightClick: true, logger);
    return true;
};
icon.Select = (hwnd, pt) =>
{
    ShowContextMenu(new HWND(hwnd.Value), pt, isRightClick: false, logger);
    return true;
};

// Run the message loop so the tray icon functions :)
logger.LogInformation("Starting message loop...");
logger.LogInformation("Press Ctrl+C to exit.");
while (PInvoke.GetMessage(out var msg, HWND.Null, 0, 0))
{
    PInvoke.TranslateMessage(msg);
    PInvoke.DispatchMessage(msg);
}

static void ShowContextMenu(HWND hwnd, PhysicalPoint pt, bool isRightClick, ILogger logger)
{
    logger.LogInformation("Showing context menu at {X},{Y}", pt.X, pt.Y);

    // Focus the window, so keyboard activation & dismissal works.
    // https://github.com/microsoft/Windows-classic-samples/blob/d338bb385b1ac47073e3540dbfa810f4dcb12ed8/Samples/Win7Samples/winui/shell/appshellintegration/NotificationIcon/NotificationIcon.cpp#L217
    PInvoke.SetForegroundWindow(hwnd);

    using var submenu = MenuHelpers.CreatePopupMenu();
    MenuHelpers.InsertMenuItem(submenu, 1, new MenuItemInfo { Text = "Welcome to the submenu!", Enabled = false });
    MenuHelpers.InsertMenuItem(submenu, 2, MenuItemInfo.CreateSeparator());
    MenuHelpers.InsertMenuItem(submenu, 3, new MenuItemInfo { Text = "&Checked option", Id = 102, Checked = true });
    MenuHelpers.InsertMenuItem(submenu, 4, new MenuItemInfo { Text = "&Highlighted option", Id = 103, Hilite = true });
    MenuHelpers.InsertMenuItem(submenu, 5, new MenuItemInfo { Text = "&Default option", Id = 104, Default = true });

    using var menu = MenuHelpers.CreatePopupMenu();

    uint exitId = 1;
    MenuHelpers.InsertMenuItem(menu, 1, new MenuItemInfo { Text = "My app", Enabled = false });
    MenuHelpers.InsertMenuItem(menu, 2, new MenuItemInfo { Text = "&Options", SubMenu = submenu });
    MenuHelpers.InsertMenuItem(menu, 3, MenuItemInfo.CreateSeparator());
    MenuHelpers.InsertMenuItem(menu, 4, new MenuItemInfo { Text = "E&xit", Id = exitId, Default = true });

    // Note: add TPM_LAYOUTRTL for RTL layouts.
    // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-trackpopupmenuex#:~:text=existing%20menu%20item.-,%5Bin%5D%20uFlags,-Type%3A%20UINT
    var flags = (TRACK_POPUP_MENU_FLAGS)MenuHelpers.GetPopupAlignmentFlags(isRightClick);
    var returnValueFlags = TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD | TRACK_POPUP_MENU_FLAGS.TPM_NONOTIFY;
    flags |= returnValueFlags;

    // Note: these are physical points; if your app is not DPI-aware, the
    // context menu will display in the wrong spot.
    //
    // See the <ApplicationManifest> entry in the csproj.
    var response = PInvoke.TrackPopupMenuEx(
        menu,
        (uint)flags,
        pt.X,
        pt.Y,
        hwnd,
        null);
    if (response == 0)
    {
        // Either nothing selected or an error occurred.
    }
    else if (response == exitId)
    {
        // Exit!
        PInvoke.PostMessage(hwnd, PInvoke.WM_CLOSE, 0, 0);
    }
    else
    {
        logger.LogInformation("Menu item {Id} selected", response);
    }
}