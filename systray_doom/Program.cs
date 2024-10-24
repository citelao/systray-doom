using System.Runtime.InteropServices;

Console.WriteLine("Hello, World!");

var i = PInvokeDoom.rust_function();
Console.WriteLine(i);

static unsafe void DrawFrame(UInt32* frame, nint xres, nint yres)
{
    // Console.WriteLine("DrawFrame");
}
static unsafe PInvokeDoom.CKeyData* KeyCallback()
{
    // Console.WriteLine("KeyCallback");
    return null;
}
static unsafe void SetWindowTitle(byte* title, nint size)
{
    Console.WriteLine($"SetWindowTitle  {System.Text.Encoding.UTF8.GetString(title, (int)size)}");
}

unsafe
{
    var game = PInvokeDoom.create_game(
        DrawFrame,
        KeyCallback,
        SetWindowTitle
    );

    PInvokeDoom.start_game(game);
}

