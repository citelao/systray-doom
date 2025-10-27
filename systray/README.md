# citelao.Systray

This is an unopinionated library for creating **systray icons** in dotnet, without needing to use WinForms or WPF.

It's designed to be as simple as possible while still being correct.

## Features

- **Very simple**: a very lightweight wrapper over the `Shell_NotifyIcon` APIs: it does not add WinForms or WPF to your code.
- **Flexible**: automatically processes & routes events for you, or you can use your own WndProc.
- **Correct**: re-registers on explorer.exe crashes & uses clear `PhysicalPoint` types to indicate physical vs scaled pixel locations.

## Getting started

```pwsh
> dotnet add package citelao.SystrayIcon
```

See more [on Nuget for citelao.SystrayIcon](https://www.nuget.org/packages/citelao.SystrayIcon/)

## Example

```csharp
GUID iconGuid = /* a consistent GUID for this type of icon */;
IntPtr myHwnd = /* a new or existing HWND; the TrayIcon will subclass its wndproc. */;

var trayIcon = new TrayIcon(
    guid: iconGuid,
    ownerHwnd: new(myHwnd))
{
    Icon = new(PInvoke.LoadIcon(default, PInvoke.IDI_APPLICATION)),
    Tooltip = "Hello, Windows!",

    ContextMenu = (hwnd, pt) =>
    {
        // Show a context menu at the specified point.
    },

    Select = (hwnd, pt) =>
    {
        // Handle selection
    },
};
```

For a full example, see https://github.com/citelao/systray-doom/.

### Example with context menu

Provided that you have the following PInvokes in your code:

1. `TRACK_POPUP_MENU_FLAGS`
2. `TrackPopupMenuEx`
3. `SetForegroundWindow`
4. `PostMessage`
5. `WM_CLOSE`

```csharp
using Systray.Menus;

var trayIcon = new TrayIcon(/* ... */)
{
    ContextMenu = (hwnd, pt) =>
    {
        // Focus the window, so keyboard activation & dismissal works.
        // https://github.com/microsoft/Windows-classic-samples/blob/d338bb385b1ac47073e3540dbfa810f4dcb12ed8/Samples/Win7Samples/winui/shell/appshellintegration/NotificationIcon/NotificationIcon.cpp#L217
        PInvoke.SetForegroundWindow(new HWND(hwnd.Value));

        using var menu = MenuHelpers.CreatePopupMenu();

        uint exitId = 1;
        MenuHelpers.InsertMenuItem(menu, 1, new MenuItemInfo { Text = "My app", Enabled = false });
        MenuHelpers.InsertMenuItem(menu, 2, MenuItemInfo.CreateSeparator());
        MenuHelpers.InsertMenuItem(menu, 3, new MenuItemInfo { Text = "E&xit", Id = exitId, Default = true });

        // Note: add TPM_LAYOUTRTL for RTL layouts.
        // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-trackpopupmenuex#:~:text=existing%20menu%20item.-,%5Bin%5D%20uFlags,-Type%3A%20UINT
        var flags = (TRACK_POPUP_MENU_FLAGS)MenuHelpers.GetPopupAlignmentFlags();
        var returnValueFlags = TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD | TRACK_POPUP_MENU_FLAGS.TPM_NONOTIFY;
        flags |= returnValueFlags;

        // Note: these are physical points; if your app is not DPI-aware, the
        // context menu will display in the wrong spot.
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
    }
};
```

## Feedback

The project repo is here: https://github.com/citelao/systray-doom/.