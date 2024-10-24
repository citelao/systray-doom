using System;
using System.Runtime.InteropServices;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

// PInvoke.MessageBox(IntPtr.Zero, "Hello, World!", "Hello", 0);
var i = PInvoke.rust_function();
Console.WriteLine(i);

static void DrawFrame(IntPtr frame, nint xres, nint yres)
{
    Console.WriteLine("DrawFrame");
}
static PInvoke.CKeyData? KeyCallback()
{
    Console.WriteLine("KeyCallback");
    return null;
}
static void SetWindowTitle(IntPtr title, nint size)
{
    Console.WriteLine("SetWindowTitle");
}

PInvoke.create_game(
    DrawFrame,
    KeyCallback,
    SetWindowTitle
);

public static partial class PInvoke
{
    [LibraryImport("../rust_bindings/target/debug/systray_doom_bindings.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial Int32 rust_function();

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CKeyData
    {
        public bool pressed;
        public byte key;
    };

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DrawFrameDelegate(IntPtr frame, nint xres, nint yres);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate CKeyData? KeyCallbackDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetWindowTitleDelegate(IntPtr title, nint size);

    [LibraryImport("../rust_bindings/target/debug/systray_doom_bindings.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial IntPtr create_game(
        DrawFrameDelegate draw_frame,
        KeyCallbackDelegate key_callback,
        SetWindowTitleDelegate set_window_title
    );
}