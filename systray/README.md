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

## Feedback

The project repo is here: https://github.com/citelao/systray-doom/.