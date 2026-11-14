using System.Runtime.InteropServices;

namespace StarCitizenMiningAssistant.Infrastructure.Capture;

public static class RegionImage
{
    public static byte[]? CaptureBmp(int x, int y, int width, int height)
    {
        if (width < 8 || height < 8 || width > 8000 || height > 8000)
        {
            return null;
        }

        var screen = GetDC(IntPtr.Zero);
        var memory = CreateCompatibleDC(screen);
        var bitmap = CreateCompatibleBitmap(screen, width, height);
        var previous = SelectObject(memory, bitmap);
        try
        {
            if (!BitBlt(memory, 0, 0, width, height, screen, x, y, 0x00CC0020))
            {
                return null;
            }

            SelectObject(memory, previous);
            previous = IntPtr.Zero;

            var stride = ((width * 24 + 31) / 32) * 4;
            var pixels = new byte[stride * height];
            var header = new BitmapInfoHeader
            {
                Size = Marshal.SizeOf<BitmapInfoHeader>(),
                Width = width,
                Height = height,
                Planes = 1,
                BitCount = 24,
                ImageSize = pixels.Length
            };
            if (GetDIBits(memory, bitmap, 0, (uint)height, pixels, ref header, 0) == 0)
            {
                return null;
            }

            return EncodeBmp(width, height, pixels);
        }
        finally
        {
            if (previous != IntPtr.Zero)
            {
                SelectObject(memory, previous);
            }

            DeleteObject(bitmap);
            DeleteDC(memory);
            ReleaseDC(IntPtr.Zero, screen);
        }
    }

    public static string? SaveBmp(string label, byte[] bmp)
    {
        if (bmp.Length < 54 || (label != "signatures" && label != "rock"))
        {
            return null;
        }

        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "StarCitizenMiningAssistant",
                "captures");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, label + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".bmp");
            File.WriteAllBytes(path, bmp);
            return path;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static byte[] EncodeBmp(int width, int height, byte[] pixels)
    {
        var file = new byte[54 + pixels.Length];
        file[0] = (byte)'B';
        file[1] = (byte)'M';
        WriteInt(file, 2, file.Length);
        WriteInt(file, 10, 54);
        WriteInt(file, 14, 40);
        WriteInt(file, 18, width);
        WriteInt(file, 22, height);
        file[26] = 1;
        file[28] = 24;
        WriteInt(file, 34, pixels.Length);
        Buffer.BlockCopy(pixels, 0, file, 54, pixels.Length);
        return file;
    }

    private static void WriteInt(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr window);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr window, IntPtr dc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr dc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr dc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int width, int height);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr obj);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr dest, int x, int y, int width, int height, IntPtr source, int srcX, int srcY, int operation);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(IntPtr dc, IntPtr bitmap, uint start, uint lines, byte[] pixels, ref BitmapInfoHeader header, uint usage);

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public int Size;
        public int Width;
        public int Height;
        public short Planes;
        public short BitCount;
        public int Compression;
        public int ImageSize;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public int ColorsUsed;
        public int ColorsImportant;
    }
}
