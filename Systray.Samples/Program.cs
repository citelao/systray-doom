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
    });
});
var logger = loggerFactory.CreateLogger<Program>();

logger.LogInformation("Starting...");

using var messageWindow = new MessageOnlyWindow("Systray.Sample.Window", (hwnd, msg, wParam, lParam) =>
{
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
var icon = new TrayIcon(guid, new(messageWindow.Hwnd.Value));

// Wait for enter key
logger.LogInformation("Press Enter to exit...");
Console.ReadLine();

