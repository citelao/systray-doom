// Adapted from WinForms.
// https://github.com/dotnet/winforms/blob/53b30edce6ac9b61e868676d2f58e3ca97c44db0/src/System.Private.Windows.Core/src/Windows/Win32/PInvokeCore.SetWindowLong.cs
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Systray.NativeTypes;

internal static partial class PInvokeCore
{
    [DllImport("USER32.dll", ExactSpelling = true, EntryPoint = "SetWindowLongW", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("windows5.0")]
    private static extern nint SetWindowLongW(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, nint dwNewLong);

    [DllImport("USER32.dll", ExactSpelling = true, EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("windows5.0")]
    private static extern nint SetWindowLongPtrW(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, nint dwNewLong);

    public static nint SetWindowLong(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, nint newValue)
    {
        nint result = Environment.Is64BitProcess
            ? SetWindowLongPtrW(hWnd, nIndex, newValue)
            : SetWindowLongW(hWnd, nIndex, (int)newValue);
        GC.KeepAlive(hWnd);
        return result;
    }

    public static nint SetWindowLong(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, HWND newValue)
    {
        nint result = SetWindowLong(hWnd, nIndex, (nint)newValue);
        GC.KeepAlive(newValue);
        return result;
    }

    public static nint SetWindowLong(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, WNDPROC dwNewLong)
    {
        IntPtr pointer = Marshal.GetFunctionPointerForDelegate(dwNewLong);
        IntPtr result = SetWindowLong(hWnd, nIndex, pointer);
        GC.KeepAlive(dwNewLong);
        return result;
    }
}