namespace systray_doom;

using System.Runtime.InteropServices;

public static partial class PInvokeDoom
{
    [LibraryImport("systray_doom_bindings.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial Int32 rust_function();

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CKeyData
    {
        public bool pressed;
        public byte key;
    };

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void DrawFrameDelegate(UInt32* frame, nint xres, nint yres);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate CKeyData* KeyCallbackDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void SetWindowTitleDelegate(byte* title, nint size);

    [LibraryImport("systray_doom_bindings.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial IntPtr create_game(
        DrawFrameDelegate draw_frame,
        KeyCallbackDelegate key_callback,
        SetWindowTitleDelegate set_window_title
    );

    // pub extern "C" fn start_game(game: *mut PublicGame) {
    [LibraryImport("systray_doom_bindings.dll", SetLastError = true)]
    public static partial void start_game(IntPtr game);
}