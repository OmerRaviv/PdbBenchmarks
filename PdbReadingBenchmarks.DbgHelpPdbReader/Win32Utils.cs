using System.ComponentModel;
using System.Runtime.InteropServices;

static internal class Win32Utils
{
    public static void ThrowOnWin32Error()
    {
        var error = Marshal.GetLastWin32Error();
        if (error != 0)
            throw new Win32Exception(error);
    }
}