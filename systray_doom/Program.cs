using System;
using System.Runtime.InteropServices;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

// PInvoke.MessageBox(IntPtr.Zero, "Hello, World!", "Hello", 0);
var i = PInvoke.rust_function();
Console.WriteLine(i);

public static partial class PInvoke
{
    [LibraryImport("../rust_bindings/target/debug/systray_doom_bindings.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial Int32 rust_function();
}