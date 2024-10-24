using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

// PInvoke.MessageBox(IntPtr.Zero, "Hello, World!", "Hello", 0);
var i = PInvoke.rust_function();
Console.WriteLine(i);

static void DrawFrame(UInt32[] frame, nint xres, nint yres)
{
    Console.WriteLine("DrawFrame");
}
static unsafe PInvoke.CKeyData* KeyCallback()
{
    Console.WriteLine("KeyCallback");
    return null;
}
static void SetWindowTitle(byte[] title, nint size)
{
    Console.WriteLine("SetWindowTitle");
}

unsafe
{
    var game = PInvoke.create_game(
        DrawFrame,
        KeyCallback,
        SetWindowTitle
    );

    PInvoke.start_game(game);
}


// public static class Doom
// {
//     public static void CreateGame()
//     {
//         PInvoke.create_game(
//             DrawFrame,
//             KeyCallback,
//             SetWindowTitle
//         );
//     }
// }

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
    public delegate void DrawFrameDelegate(UInt32[] frame, nint xres, nint yres);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate CKeyData* KeyCallbackDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetWindowTitleDelegate(byte[] title, nint size);

    [LibraryImport("../rust_bindings/target/debug/systray_doom_bindings.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial IntPtr create_game(
        DrawFrameDelegate draw_frame,
        KeyCallbackDelegate key_callback,
        SetWindowTitleDelegate set_window_title
    );

    // pub extern "C" fn start_game(game: *mut PublicGame) {
    [LibraryImport("../rust_bindings/target/debug/systray_doom_bindings.dll", SetLastError = true)]
    public static partial void start_game(IntPtr game);
}