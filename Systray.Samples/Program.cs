using Systray;
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
    .SetMinimumLevel(LogLevel.Trace);
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

// Wait for enter key
// logger.LogInformation("Press Enter to exit...");
// Console.ReadLine();

logger.LogInformation("Starting message loop...");
while (PInvoke.GetMessage(out var msg, HWND.Null, 0, 0))
{
    PInvoke.TranslateMessage(msg);
    PInvoke.DispatchMessage(msg);
}