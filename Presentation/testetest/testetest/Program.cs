using System.Runtime.InteropServices;

static class NativeMethods
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int MessageBox(
        IntPtr hWnd,
        string text,
        string caption,
        uint type);
}

class Program
{
    static void Main()
    {
        NativeMethods.MessageBox(IntPtr.Zero, "Hi", "Title", 0);
    }
}

