using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace StarCitizenMiningAssistant.App;

internal static class WindowClickThrough
{
    private const int ExtendedStyle = -20;
    private const int TransparentHitTest = 0x00000020;

    public static void Apply(Window window, bool enabled)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var style = GetExtendedStyle(handle).ToInt64();
        var next = enabled ? style | TransparentHitTest : style & ~TransparentHitTest;
        SetExtendedStyle(handle, new IntPtr(next));
        SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, 0x0027);
    }

    private static IntPtr GetExtendedStyle(IntPtr handle) =>
        IntPtr.Size == 8 ? GetWindowLongPtr64(handle, ExtendedStyle) : new IntPtr(GetWindowLong32(handle, ExtendedStyle));

    private static void SetExtendedStyle(IntPtr handle, IntPtr style)
    {
        if (IntPtr.Size == 8)
        {
            SetWindowLongPtr64(handle, ExtendedStyle, style);
        }
        else
        {
            SetWindowLong32(handle, ExtendedStyle, style.ToInt32());
        }
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLong", CharSet = CharSet.Auto)]
    private static extern int GetWindowLong32(IntPtr handle, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Auto)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr handle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Auto)]
    private static extern int SetWindowLong32(IntPtr handle, int index, int style);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Auto)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr handle, int index, IntPtr style);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr handle, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
