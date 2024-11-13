public static class PInvokeHelpers
{
    public static nint LOWORD(nint n)
    {
        return n & 0xFFFF;
    }

    public static nint HIWORD(nint n)
    {
        return n >> 16;
    }

    // TODO: verify that we don't get negative coords.
    public static int GET_X_LPARAM(nuint wParam)
    {
        return (int)(wParam & 0xFFFF);
    }

    public static int GET_Y_LPARAM(nuint wParam)
    {
        return (int)(wParam >> 16);
    }
}