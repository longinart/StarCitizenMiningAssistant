using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace StarCitizenMiningAssistant.App;

internal static class DarkCaption
{
    public static void Apply(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var dark = 1;
        var caption = 0x001D1916;
        var text = 0x00EBE8E6;
        DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));
        DwmSetWindowAttribute(handle, 35, ref caption, sizeof(int));
        DwmSetWindowAttribute(handle, 36, ref text, sizeof(int));
        SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, 0x0027);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
