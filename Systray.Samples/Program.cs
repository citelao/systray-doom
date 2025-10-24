using Systray;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

Console.WriteLine("Hello, World!");

using var messageWindow = new MessageOnlyWindow("Systray.Sample.Window");

// Wait for enter key
Console.WriteLine("Press Enter to exit...");
Console.ReadLine();
