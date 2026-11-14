namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// Finds the bright signature digits in a captured rectangle. The location pin
/// is the taller mark on the left. Ship color is not used. Decision 018.
/// </summary>
public static class SignatureDigitStrip
{
    public readonly record struct Prepared(byte[] Smooth, byte[] Sharp)
    {
        public bool Found => Smooth.Length > 0 && Sharp.Length > 0;

        public static Prepared Empty { get; } = new([], []);
    }

    public static Prepared Prepare(ReadOnlySpan<byte> bmp)
    {
        if (!TryBrightness(bmp, out var brightness, out var width, out var height))
        {
            return Prepared.Empty;
        }

        var threshold = Math.Clamp(Median(brightness) + 55, 80, 210);
        var ink = new bool[brightness.Length];
        var inkCount = 0;
        for (var i = 0; i < brightness.Length; i++)
        {
            ink[i] = brightness[i] >= threshold;
            if (ink[i])
            {
                inkCount++;
            }
        }

        if (inkCount < 12)
        {
            return Prepared.Empty;
        }

        var marks = Marks(ink, width, height);
        var kept = DigitMarks(marks, width);
        if (kept.Count == 0)
        {
            return Prepared.Empty;
        }

        var left = Math.Max(0, kept.Min(mark => mark.Left) - 4);
        var top = Math.Max(0, kept.Min(mark => mark.Top) - 4);
        var right = Math.Min(width - 1, kept.Max(mark => mark.Right) + 4);
        var bottom = Math.Min(height - 1, kept.Max(mark => mark.Bottom) + 4);
        var cropWidth = right - left + 1;
        var cropHeight = bottom - top + 1;
        var crop = new byte[cropWidth * cropHeight];
        for (var y = 0; y < cropHeight; y++)
        {
            for (var x = 0; x < cropWidth; x++)
            {
                crop[(y * cropWidth) + x] = brightness[((top + y) * width) + left + x];
            }
        }

        return new Prepared(Render(crop, cropWidth, cropHeight, 56, smooth: true), Render(crop, cropWidth, cropHeight, 96, smooth: false));
    }

    private readonly record struct Mark(int Left, int Top, int Right, int Bottom);

    private static List<Mark> DigitMarks(List<Mark> marks, int width)
    {
        if (marks.Count == 0)
        {
            return marks;
        }

        var tallest = marks.Max(mark => mark.Bottom - mark.Top + 1);
        var kept = marks
            .Where(mark =>
            {
                var markHeight = mark.Bottom - mark.Top + 1;
                var markWidth = mark.Right - mark.Left + 1;
                return markHeight >= tallest * 0.45 && markWidth >= 2 && markWidth < width * 0.8;
            })
            .OrderBy(mark => mark.Left)
            .ToList();
        if (kept.Count < 2)
        {
            return kept;
        }

        var restHeights = kept.Skip(1).Select(mark => mark.Bottom - mark.Top + 1).Order().ToArray();
        var medianRest = restHeights[restHeights.Length / 2];
        var firstHeight = kept[0].Bottom - kept[0].Top + 1;
        var gap = kept[1].Left - kept[0].Right - 1;
        var laterGaps = new List<int>();
        for (var i = 2; i < kept.Count; i++)
        {
            laterGaps.Add(kept[i].Left - kept[i - 1].Right - 1);
        }

        var medianLater = laterGaps.Count == 0 ? 0 : laterGaps.Order().ElementAt(laterGaps.Count / 2);
        var taller = firstHeight > medianRest * 1.25;
        var detached = firstHeight > medianRest && gap >= 6 && (laterGaps.Count == 0 || gap > medianLater * 3);
        if (taller || detached)
        {
            kept.RemoveAt(0);
        }

        return kept;
    }

    private static byte[] Render(byte[] crop, int cropWidth, int cropHeight, int targetHeight, bool smooth)
    {
        var low = (int)crop.Min();
        var high = (int)crop.Max();
        var span = Math.Max(1, high - low);
        var outHeight = targetHeight;
        var outWidth = Math.Max(1, (int)Math.Round(cropWidth * (outHeight / (double)cropHeight)));
        const int margin = 16;
        var fullWidth = outWidth + (margin * 2);
        var fullHeight = outHeight + (margin * 2);
        var pixels = new byte[fullWidth * fullHeight];
        Array.Fill(pixels, (byte)255);
        for (var y = 0; y < outHeight; y++)
        {
            for (var x = 0; x < outWidth; x++)
            {
                var value = smooth
                    ? SampleSmooth(crop, cropWidth, cropHeight, x, y, outWidth, outHeight)
                    : crop[(Math.Min(cropHeight - 1, y * cropHeight / outHeight) * cropWidth) + Math.Min(cropWidth - 1, x * cropWidth / outWidth)];
                var stretched = (int)Math.Clamp((value - low) * 255 / span, 0, 255);
                pixels[((y + margin) * fullWidth) + x + margin] = (byte)(255 - stretched);
            }
        }

        return Bmp(pixels, fullWidth, fullHeight);
    }

    private static double SampleSmooth(byte[] crop, int cropWidth, int cropHeight, int x, int y, int outWidth, int outHeight)
    {
        var sy = cropHeight == 1 ? 0 : y * (cropHeight - 1) / (double)Math.Max(1, outHeight - 1);
        var sx = cropWidth == 1 ? 0 : x * (cropWidth - 1) / (double)Math.Max(1, outWidth - 1);
        var x0 = (int)Math.Floor(sx);
        var y0 = (int)Math.Floor(sy);
        var x1 = Math.Min(cropWidth - 1, x0 + 1);
        var y1 = Math.Min(cropHeight - 1, y0 + 1);
        var tx = sx - x0;
        var ty = sy - y0;
        return (crop[(y0 * cropWidth) + x0] * (1 - tx) * (1 - ty))
            + (crop[(y0 * cropWidth) + x1] * tx * (1 - ty))
            + (crop[(y1 * cropWidth) + x0] * (1 - tx) * ty)
            + (crop[(y1 * cropWidth) + x1] * tx * ty);
    }

    private static List<Mark> Marks(bool[] ink, int width, int height)
    {
        var labels = new int[ink.Length];
        var parent = new List<int>();
        int Find(int id)
        {
            while (parent[id] != id)
            {
                parent[id] = parent[parent[id]];
                id = parent[id];
            }

            return id;
        }

        var next = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = (y * width) + x;
                if (!ink[index])
                {
                    continue;
                }

                var left = x > 0 && ink[index - 1] ? Find(labels[index - 1]) : -1;
                var up = y > 0 && ink[index - width] ? Find(labels[index - width]) : -1;
                if (left < 0 && up < 0)
                {
                    parent.Add(next);
                    labels[index] = next++;
                }
                else if (left < 0 || up < 0 || left == up)
                {
                    labels[index] = left >= 0 ? left : up;
                }
                else
                {
                    parent[up] = left;
                    labels[index] = left;
                }
            }
        }

        var bounds = new Dictionary<int, Mark>();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = (y * width) + x;
                if (!ink[index])
                {
                    continue;
                }

                var id = Find(labels[index]);
                if (!bounds.TryGetValue(id, out var mark))
                {
                    bounds[id] = new Mark(x, y, x, y);
                }
                else
                {
                    bounds[id] = new Mark(Math.Min(mark.Left, x), Math.Min(mark.Top, y), Math.Max(mark.Right, x), Math.Max(mark.Bottom, y));
                }
            }
        }

        return bounds.Values.ToList();
    }

    private static int Median(byte[] values)
    {
        var copy = values.ToArray();
        Array.Sort(copy);
        return copy[copy.Length / 2];
    }

    private static bool TryBrightness(ReadOnlySpan<byte> image, out byte[] brightness, out int width, out int height)
    {
        brightness = [];
        width = 0;
        height = 0;
        if (image.Length < 54 || image[0] != (byte)'B' || image[1] != (byte)'M' || BitConverter.ToInt16(image.Slice(28)) != 24)
        {
            return false;
        }

        var pixelOffset = BitConverter.ToInt32(image.Slice(10));
        width = BitConverter.ToInt32(image.Slice(18));
        height = BitConverter.ToInt32(image.Slice(22));
        var stride = ((width * 3) + 3) / 4 * 4;
        if (width < 8 || height < 8 || pixelOffset < 54 || pixelOffset + (stride * height) > image.Length)
        {
            brightness = [];
            width = 0;
            height = 0;
            return false;
        }

        brightness = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            var fileRow = height - 1 - y;
            for (var x = 0; x < width; x++)
            {
                var index = pixelOffset + (fileRow * stride) + (x * 3);
                brightness[(y * width) + x] = (byte)Math.Max(image[index], Math.Max(image[index + 1], image[index + 2]));
            }
        }

        return true;
    }

    private static byte[] Bmp(byte[] gray, int width, int height)
    {
        var stride = ((width * 3) + 3) / 4 * 4;
        var pixels = new byte[stride * height];
        for (var y = 0; y < height; y++)
        {
            var fileRow = height - 1 - y;
            for (var x = 0; x < width; x++)
            {
                var value = gray[(y * width) + x];
                var index = (fileRow * stride) + (x * 3);
                pixels[index] = value;
                pixels[index + 1] = value;
                pixels[index + 2] = value;
            }
        }

        var file = new byte[54 + pixels.Length];
        file[0] = (byte)'B';
        file[1] = (byte)'M';
        Write(file, 2, file.Length);
        Write(file, 10, 54);
        Write(file, 14, 40);
        Write(file, 18, width);
        Write(file, 22, height);
        file[26] = 1;
        file[28] = 24;
        Write(file, 34, pixels.Length);
        pixels.CopyTo(file.AsSpan(54));
        return file;
    }

    private static void Write(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }
}
