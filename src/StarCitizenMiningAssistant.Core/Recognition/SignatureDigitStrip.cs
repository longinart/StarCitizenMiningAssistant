namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// Finds the bright signature digits in a captured rectangle. The location pin
/// is the taller mark on the left. Ship color is not used. Decision 018.
/// </summary>
public static class SignatureDigitStrip
{
    public readonly record struct Prepared(byte[] Smooth, byte[] Sharp, string? Shapes)
    {
        public bool Found => Smooth.Length > 0 && Sharp.Length > 0;

        public static Prepared Empty { get; } = new([], [], null);
    }

    public static Prepared Prepare(ReadOnlySpan<byte> bmp)
    {
        if (!TryBrightness(bmp, out var maxChannel, out var minChannel, out var width, out var height))
        {
            return Prepared.Empty;
        }

        var brightness = ChooseField(maxChannel, minChannel, out var threshold, out var brightGround);
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

        var marks = PillRun(Marks(ink, width, height));
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

        var shapes = brightGround ? ReadShapes(crop, cropWidth, cropHeight, threshold) : null;
        return new Prepared(
            Render(crop, cropWidth, cropHeight, brightGround ? 64 : 56, smooth: true, brightGround, threshold),
            Render(crop, cropWidth, cropHeight, brightGround ? 80 : 96, smooth: false, brightGround, threshold),
            shapes);
    }

    private static string? ReadShapes(byte[] crop, int cropWidth, int cropHeight, int threshold)
    {
        var ink = new byte[crop.Length];
        for (var i = 0; i < crop.Length; i++)
        {
            ink[i] = crop[i] >= threshold ? (byte)255 : (byte)0;
        }

        var line = HudNumberReader.Read(ink, cropWidth, cropHeight).FirstOrDefault();
        return string.IsNullOrEmpty(line) ? null : line;
    }

    private static byte[] ChooseField(byte[] maxChannel, byte[] minChannel, out int threshold, out bool brightGround)
    {
        var median = Median(maxChannel);
        var maxTail = BrightTail(maxChannel);
        var minTail = BrightTail(minChannel);
        if (median >= 160 && (maxTail.Count > 0 || minTail.Count > 0))
        {
            brightGround = true;
            var useMin = minTail.Count > 0;
            if (useMin)
            {
                threshold = minTail.Cut;
                return minChannel;
            }

            threshold = maxTail.Cut;
            return maxChannel;
        }

        brightGround = false;
        threshold = Math.Clamp(median + 55, 80, 210);
        return maxChannel;
    }

    private readonly record struct Tail(int Cut, int Gap, int Count);

    private static Tail BrightTail(byte[] field)
    {
        var histogram = new int[256];
        foreach (var value in field)
        {
            histogram[value]++;
        }

        var dense = new List<int>();
        for (var value = 0; value < histogram.Length; value++)
        {
            if (histogram[value] > field.Length * 0.002)
            {
                dense.Add(value);
            }
        }

        var best = new Tail(0, 0, 0);
        for (var i = 1; i < dense.Count; i++)
        {
            var gap = dense[i] - dense[i - 1];
            if (gap < 8 || dense[i - 1] < 160)
            {
                continue;
            }

            var count = 0;
            for (var value = dense[i - 1] + 1; value < histogram.Length; value++)
            {
                count += histogram[value];
            }

            var fraction = count / (double)field.Length;
            if (fraction < 0.004 || fraction > 0.15 || count <= best.Count)
            {
                continue;
            }

            var cut = gap >= 20
                ? dense[i - 1] + 2
                : dense[i - 1] + Math.Max(1, gap / 2);
            best = new Tail(cut, gap, count);
        }

        return best;
    }

    private static List<Mark> PillRun(List<Mark> marks)
    {
        if (marks.Count <= 1)
        {
            return marks;
        }

        List<Mark>? best = null;
        var bestScore = 0;
        foreach (var band in Bands(marks))
        {
            foreach (var run in Runs(band))
            {
                var heights = run.Select(mark => mark.Bottom - mark.Top + 1).Order().ToArray();
                var medianHeight = heights[heights.Length / 2];
                if (medianHeight < 6)
                {
                    continue;
                }

                var digits = run.Count(mark =>
                {
                    var markHeight = mark.Bottom - mark.Top + 1;
                    var markWidth = mark.Right - mark.Left + 1;
                    return markHeight >= medianHeight * 0.6 && markWidth >= 2 && markWidth < markHeight * 1.8;
                });
                var leftHeight = run[0].Bottom - run[0].Top + 1;
                var pin = leftHeight > medianHeight * 1.2 ? 3 : 0;
                var score = (digits * 10) + pin + medianHeight;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = run;
                }
            }
        }

        return best ?? marks;
    }

    private static List<List<Mark>> Bands(List<Mark> marks)
    {
        var bands = new List<List<Mark>>();
        foreach (var mark in marks.OrderBy(item => item.Top))
        {
            var placed = false;
            var middle = (mark.Top + mark.Bottom) / 2.0;
            foreach (var band in bands)
            {
                var bandMiddle = band.Average(item => (item.Top + item.Bottom) / 2.0);
                var bandHeight = band.Average(item => item.Bottom - item.Top + 1);
                if (Math.Abs(middle - bandMiddle) <= bandHeight * 0.75)
                {
                    band.Add(mark);
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                bands.Add([mark]);
            }
        }

        return bands;
    }

    private static List<List<Mark>> Runs(List<Mark> band)
    {
        var row = band.OrderBy(mark => mark.Left).ToList();
        var runs = new List<List<Mark>>();
        var current = new List<Mark> { row[0] };
        for (var i = 1; i < row.Count; i++)
        {
            var gap = row[i].Left - current[^1].Right - 1;
            var typical = current.Average(mark => mark.Right - mark.Left + 1);
            if (gap > Math.Max(10, typical * 1.6))
            {
                runs.Add(current);
                current = [];
            }

            current.Add(row[i]);
        }

        runs.Add(current);
        return runs;
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

    private static byte[] Render(byte[] crop, int cropWidth, int cropHeight, int targetHeight, bool smooth, bool binary, int inkThreshold)
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
                byte shown;
                if (binary)
                {
                    shown = value >= inkThreshold ? (byte)0 : (byte)255;
                }
                else
                {
                    var stretched = (int)Math.Clamp((value - low) * 255 / span, 0, 255);
                    shown = (byte)(255 - stretched);
                }

                pixels[((y + margin) * fullWidth) + x + margin] = shown;
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

    private static bool TryBrightness(ReadOnlySpan<byte> image, out byte[] maxChannel, out byte[] minChannel, out int width, out int height)
    {
        maxChannel = [];
        minChannel = [];
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
            width = 0;
            height = 0;
            return false;
        }

        maxChannel = new byte[width * height];
        minChannel = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            var fileRow = height - 1 - y;
            for (var x = 0; x < width; x++)
            {
                var index = pixelOffset + (fileRow * stride) + (x * 3);
                var blue = image[index];
                var green = image[index + 1];
                var red = image[index + 2];
                var pixel = (y * width) + x;
                maxChannel[pixel] = (byte)Math.Max(red, Math.Max(green, blue));
                minChannel[pixel] = (byte)Math.Min(red, Math.Min(green, blue));
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
