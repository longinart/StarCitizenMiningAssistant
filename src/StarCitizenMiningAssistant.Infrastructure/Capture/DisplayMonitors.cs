using System.Runtime.InteropServices;

namespace StarCitizenMiningAssistant.Infrastructure.Capture;

public static class DisplayMonitors
{
    public static IReadOnlyList<MonitorInfo> List()
    {
        var found = new List<MonitorInfo>();
        MonitorEnumProc callback = (IntPtr monitor, IntPtr _, ref Rect _, IntPtr _) =>
        {
            var info = new MonitorInfoEx { Size = Marshal.SizeOf<MonitorInfoEx>() };
            if (!GetMonitorInfo(monitor, ref info))
            {
                return true;
            }

            var width = info.Monitor.Right - info.Monitor.Left;
            var height = info.Monitor.Bottom - info.Monitor.Top;
            if (width > 0 && height > 0 && !string.IsNullOrWhiteSpace(info.DeviceName))
            {
                found.Add(new MonitorInfo(
                    info.DeviceName,
                    info.Monitor.Left,
                    info.Monitor.Top,
                    width,
                    height,
                    (info.Flags & 1) != 0));
            }

            return true;
        };

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
        return found;
    }

    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, ref Rect rect, IntPtr data);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfoEx info);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfoEx
    {
        public int Size;
        public Rect Monitor;
        public Rect Work;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }
}
