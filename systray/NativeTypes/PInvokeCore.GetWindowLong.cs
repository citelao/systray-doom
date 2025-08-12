// Adapted from WinForms.
// https://github.com/dotnet/winforms/blob/53b30edce6ac9b61e868676d2f58e3ca97c44db0/src/System.Private.Windows.Core/src/Windows/Win32/PInvokeCore.GetWindowLong.cs
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
    [DllImport("USER32.dll", ExactSpelling = true, EntryPoint = "GetWindowLongW", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("windows5.0")]
    private static extern nint GetWindowLongW(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex);

    [DllImport("USER32.dll", ExactSpelling = true, EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("windows5.0")]
    private static extern nint GetWindowLongPtrW(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex);

    /// <summary>
    ///  Dynamic wrapper for GetWindowLong that works on both 32 and 64 bit.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   <see href="https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-getwindowlongptrw">
    ///    GetWindowLong documentation.
    ///   </see>
    ///  </para>
    /// </remarks>
    /// <returns></returns>
    public static nint GetWindowLong(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex)
    {
        nint result = Environment.Is64BitProcess
            ? GetWindowLongPtrW(hWnd, nIndex)
            : GetWindowLongW(hWnd, nIndex);
        GC.KeepAlive(hWnd);
        return result;
    }
}