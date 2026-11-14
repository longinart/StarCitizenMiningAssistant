namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// Reads the bright core of game-interface digits. Windows OCR misses this
/// font: the glyphs are small and the colored edge splits each stroke.
/// A result is still only a candidate. It is not a confirmed reading.
/// </summary>
public readonly record struct HudInspection(IReadOnlyList<string> Lines, bool Clipped);

public static class HudNumberReader
{
    public static IReadOnlyList<string> Read(ReadOnlySpan<byte> luminance, int width, int height) =>
        Inspect(luminance, width, height).Lines;

    public static HudInspection Inspect(ReadOnlySpan<byte> luminance, int width, int height)
    {
        if (width < 8 || height < 8 || luminance.Length < width * height)
        {
            return new HudInspection([], false);
        }

        var max = 0;
        for (var i = 0; i < width * height; i++)
        {
            if (luminance[i] > max)
            {
                max = luminance[i];
            }
        }

        if (max < 80)
        {
            return new HudInspection([], false);
        }

        var threshold = max - 25;
        var ink = new bool[width * height];
        for (var i = 0; i < ink.Length; i++)
        {
            ink[i] = luminance[i] >= threshold;
        }

        string? best = null;
        var bestDigits = 0;
        var bestInk = 0;
        var bestClipped = false;
        var densest = 0;
        var densestClipped = false;
        foreach (var band in Bands(ink, width, height))
        {
            if (!TryInkBounds(ink, width, band.Top, band.Bottom, out var left, out var right, out var top, out var bottom, out var count))
            {
                continue;
            }

            var touches = left == 0 || right == width - 1 || top == 0 || bottom == height - 1;
            if (count > densest)
            {
                densest = count;
                densestClipped = touches;
            }

            var text = ReadBand(ink, width, band.Top, band.Bottom);
            var digits = text.Count(char.IsDigit);
            if (digits == 0 || (best is not null && (digits < bestDigits || (digits == bestDigits && count <= bestInk))))
            {
                continue;
            }

            best = text;
            bestDigits = digits;
            bestInk = count;
            bestClipped = touches;
        }

        return best is null
            ? new HudInspection([], densestClipped)
            : new HudInspection([best], bestClipped);
    }

    private static string ReadBand(bool[] ink, int width, int top, int bottom)
    {
        var glyphs = new List<Glyph>();
        var start = -1;
        var rowHeight = bottom - top + 1;
        for (var x = 0; x <= width; x++)
        {
            var column = x < width && ColumnHasInk(ink, width, x, top, bottom);
            if (column && start < 0)
            {
                start = x;
            }
            else if (!column && start >= 0)
            {
                if (TryCrop(ink, width, start, x - 1, top, bottom, out var glyph) && glyph.Ink >= 4)
                {
                    glyphs.Add(glyph);
                }

                start = -1;
            }
        }

        var tall = glyphs.Where(glyph => glyph.Height >= 8).Select(glyph => glyph.Height).Order().ToList();
        var digitHeight = tall.Count == 0 ? rowHeight : tall[tall.Count / 2];
        var wide = glyphs.Where(glyph => glyph.Height >= digitHeight * 0.7).Select(glyph => glyph.Width).Order().ToList();
        var digitWidth = wide.Count == 0 ? digitHeight : wide[wide.Count / 2];
        var text = new List<char>();
        foreach (var glyph in glyphs)
        {
            if (glyph.Height > digitHeight * 1.35
                || glyph.Width > digitWidth * 1.6
                || glyph.Width > glyph.Height * 0.85)
            {
                continue;
            }

            var symbol = Classify(glyph, digitHeight);
            if (symbol is not null)
            {
                text.Add(symbol.Value);
            }
        }

        return new string(text.ToArray()).Replace(",", "");
    }

    private static char? Classify(Glyph glyph, int digitHeight)
    {
        var center = glyph.Top + (glyph.Height / 2.0);
        var bandCenter = glyph.BandTop + ((glyph.BandBottom - glyph.BandTop) / 2.0);
        if (glyph.Height <= digitHeight * 0.55 && glyph.Width <= digitHeight * 0.55 && center > bandCenter)
        {
            return ',';
        }

        if (glyph.Height < 8)
        {
            return null;
        }

        var holes = Holes(glyph);
        if (holes.Count >= 2 || (holes.Count == 1 && IsEight(glyph)))
        {
            return '8';
        }

        if (holes.Count == 1)
        {
            var relative = holes[0] / glyph.Height;
            if (relative < 0.32)
            {
                return '9';
            }

            if (relative > 0.68)
            {
                return '6';
            }

            return '0';
        }

        if (IsFive(glyph))
        {
            return '5';
        }

        if (IsThree(glyph))
        {
            return '3';
        }

        if (IsTwo(glyph))
        {
            return '2';
        }

        if (IsSeven(glyph))
        {
            return '7';
        }

        if (IsFour(glyph))
        {
            return '4';
        }

        if (IsOne(glyph))
        {
            return '1';
        }

        return null;
    }

    private readonly record struct RowScan(double Fraction, double Mass, bool Any);

    private static RowScan ScanRow(Glyph glyph, int y)
    {
        var sum = 0;
        var count = 0;
        var span = Math.Max(1, glyph.Width - 1);
        for (var x = glyph.Left; x <= glyph.Right; x++)
        {
            if (!glyph.IsInk(x, y))
            {
                continue;
            }

            sum += x - glyph.Left;
            count++;
        }

        if (count == 0)
        {
            return new RowScan(0, 0.5, false);
        }

        return new RowScan(count / (double)glyph.Width, (sum / (double)count) / span, true);
    }

    private static bool IsEight(Glyph glyph)
    {
        var waist = -1;
        var from = glyph.Top + (int)(glyph.Height * 0.3);
        var to = glyph.Top + (int)(glyph.Height * 0.7);
        for (var y = from; y <= to; y++)
        {
            if (CenterSolid(glyph, y))
            {
                waist = y;
                break;
            }
        }

        if (waist < 0)
        {
            return false;
        }

        var above = false;
        var below = false;
        for (var y = glyph.Top; y < waist; y++)
        {
            above |= CenterOpen(glyph, y);
        }

        for (var y = waist + 1; y <= glyph.Bottom; y++)
        {
            below |= CenterOpen(glyph, y);
        }

        return above && below;
    }

    private static bool CenterOpen(Glyph glyph, int y)
    {
        var seenInk = false;
        var gapAfterInk = false;
        for (var x = glyph.Left; x <= glyph.Right; x++)
        {
            if (glyph.IsInk(x, y))
            {
                if (gapAfterInk)
                {
                    return true;
                }

                seenInk = true;
            }
            else if (seenInk)
            {
                gapAfterInk = true;
            }
        }

        return false;
    }

    private static bool CenterSolid(Glyph glyph, int y)
    {
        if (CenterOpen(glyph, y))
        {
            return false;
        }

        var ink = 0;
        for (var x = glyph.Left; x <= glyph.Right; x++)
        {
            if (glyph.IsInk(x, y))
            {
                ink++;
            }
        }

        return ink >= glyph.Width * 0.65;
    }

    private static bool IsOne(Glyph glyph)
    {
        var min = 1.0;
        for (var y = glyph.Top; y <= glyph.Bottom; y++)
        {
            var ink = 0;
            for (var x = glyph.Left; x <= glyph.Right; x++)
            {
                if (glyph.IsInk(x, y))
                {
                    ink++;
                }
            }

            var fraction = ink / (double)glyph.Width;
            if (fraction < min)
            {
                min = fraction;
            }
        }

        return min >= 0.45;
    }

    private static bool IsSeven(Glyph glyph)
    {
        var topInk = 0;
        var topCells = 0;
        var topEnd = glyph.Top + (int)(glyph.Height * 0.28);
        for (var y = glyph.Top; y <= topEnd; y++)
        {
            for (var x = glyph.Left; x <= glyph.Right; x++)
            {
                topCells++;
                if (glyph.IsInk(x, y))
                {
                    topInk++;
                }
            }
        }

        if (topCells == 0 || topInk < topCells * 0.55 || ScanRow(glyph, glyph.Top).Fraction < 0.7 || CenterOpen(glyph, glyph.Top))
        {
            return false;
        }

        var bottomInk = 0;
        var bottomCells = 0;
        var bottomStart = glyph.Top + (int)(glyph.Height * 0.7);
        for (var y = bottomStart; y <= glyph.Bottom; y++)
        {
            for (var x = glyph.Left; x <= glyph.Right; x++)
            {
                bottomCells++;
                if (glyph.IsInk(x, y))
                {
                    bottomInk++;
                }
            }
        }

        return bottomCells > 0 && bottomInk > 0 && bottomInk < bottomCells * 0.45;
    }

    private static bool IsTwo(Glyph glyph)
    {
        var bottomInk = 0;
        var bottomCells = 0;
        var bottomStart = glyph.Top + (int)(glyph.Height * 0.75);
        for (var y = bottomStart; y <= glyph.Bottom; y++)
        {
            for (var x = glyph.Left; x <= glyph.Right; x++)
            {
                bottomCells++;
                if (glyph.IsInk(x, y))
                {
                    bottomInk++;
                }
            }
        }

        if (bottomCells == 0 || bottomInk < bottomCells * 0.55)
        {
            return false;
        }

        var rightHook = false;
        var leftHook = false;
        var leftStem = false;
        for (var y = glyph.Top; y <= glyph.Bottom; y++)
        {
            var row = ScanRow(glyph, y);
            if (!row.Any)
            {
                continue;
            }

            var relative = (y - glyph.Top) / (double)glyph.Height;
            if (relative < 0.45 && row.Mass < 0.35 && row.Fraction < 0.8)
            {
                leftStem = true;
            }

            if (relative < 0.55 && row.Mass > 0.62)
            {
                rightHook = true;
            }

            if (relative is > 0.45 and < 0.85 && row.Mass < 0.42)
            {
                leftHook = true;
            }
        }

        return rightHook && leftHook && !leftStem;
    }

    private static bool IsFive(Glyph glyph)
    {
        var topEnd = glyph.Top + (int)(glyph.Height * 0.34);
        var bottomStart = glyph.Bottom - (int)(glyph.Height * 0.22);
        if (!HasBar(glyph, glyph.Top, topEnd, 0.65) || !HasBar(glyph, bottomStart, glyph.Bottom, 0.62))
        {
            return false;
        }

        var leftStem = false;
        var rightStem = false;
        for (var y = glyph.Top; y <= glyph.Bottom; y++)
        {
            var row = ScanRow(glyph, y);
            if (!row.Any)
            {
                continue;
            }

            var relative = (y - glyph.Top) / (double)glyph.Height;
            if (relative < 0.45 && row.Mass < 0.35 && row.Fraction < 0.8)
            {
                leftStem = true;
            }

            if (relative is > 0.45 and < 0.82 && row.Mass > 0.7 && row.Fraction < 0.75)
            {
                rightStem = true;
            }
        }

        return leftStem && rightStem;
    }

    private static bool IsThree(Glyph glyph)
    {
        if (ScanRow(glyph, glyph.Top).Fraction < 0.7)
        {
            return false;
        }

        var upperRight = false;
        var lowerRight = false;
        var leftStem = false;
        var bottomInk = 0;
        var bottomCells = 0;
        var bottomStart = glyph.Top + (int)(glyph.Height * 0.75);
        for (var y = glyph.Top; y <= glyph.Bottom; y++)
        {
            var row = ScanRow(glyph, y);
            var relative = (y - glyph.Top) / (double)glyph.Height;
            if (row.Any && relative < 0.45 && row.Mass < 0.35 && row.Fraction < 0.8)
            {
                leftStem = true;
            }

            if (row.Any && relative is > 0.12 and < 0.48 && row.Mass > 0.65 && row.Fraction < 0.75)
            {
                upperRight = true;
            }

            if (row.Any && relative is > 0.5 and < 0.82 && row.Mass > 0.65 && row.Fraction < 0.75)
            {
                lowerRight = true;
            }

            if (y >= bottomStart)
            {
                bottomCells += glyph.Width;
                bottomInk += (int)Math.Round(row.Fraction * glyph.Width);
            }
        }

        return !leftStem && upperRight && lowerRight && bottomCells > 0 && bottomInk >= bottomCells * 0.5;
    }

    private static bool HasBar(Glyph glyph, int from, int to, double fraction)
    {
        for (var y = from; y <= to; y++)
        {
            if (ScanRow(glyph, y).Fraction >= fraction)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsFour(Glyph glyph)
    {
        var bottomStart = glyph.Bottom - (int)(glyph.Height * 0.22);
        if (HasBar(glyph, bottomStart, glyph.Bottom, 0.62))
        {
            return false;
        }

        var top = ScanRow(glyph, glyph.Top);
        if (top.Fraction >= 0.7 && !CenterOpen(glyph, glyph.Top))
        {
            return false;
        }

        var bar = false;
        var stem = false;
        var left = false;
        for (var y = glyph.Top; y <= glyph.Bottom; y++)
        {
            var row = ScanRow(glyph, y);
            if (!row.Any)
            {
                continue;
            }

            var relative = (y - glyph.Top) / (double)glyph.Height;
            if (relative is >= 0.28 and <= 0.62 && row.Fraction >= 0.8)
            {
                bar = true;
            }

            if (relative > 0.62 && row.Mass > 0.65 && row.Fraction < 0.55)
            {
                stem = true;
            }

            if (relative < 0.4 && row.Mass < 0.5 && row.Fraction is > 0.2 and < 0.75)
            {
                left = true;
            }
        }

        return bar && stem && left;
    }

    private static List<double> Holes(Glyph glyph)
    {
        var width = glyph.Width + 2;
        var height = glyph.Height + 2;
        var blocked = new bool[width * height];
        for (var y = 0; y < glyph.Height; y++)
        {
            for (var x = 0; x < glyph.Width; x++)
            {
                if (glyph.IsInk(glyph.Left + x, glyph.Top + y))
                {
                    blocked[(y + 1) * width + (x + 1)] = true;
                }
            }
        }

        var outside = new bool[width * height];
        var queue = new Queue<(int X, int Y)>();
        void Push(int x, int y)
        {
            if ((uint)x >= (uint)width || (uint)y >= (uint)height)
            {
                return;
            }

            var index = y * width + x;
            if (outside[index] || blocked[index])
            {
                return;
            }

            outside[index] = true;
            queue.Enqueue((x, y));
        }

        for (var x = 0; x < width; x++)
        {
            Push(x, 0);
            Push(x, height - 1);
        }

        for (var y = 0; y < height; y++)
        {
            Push(0, y);
            Push(width - 1, y);
        }

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            Push(x - 1, y);
            Push(x + 1, y);
            Push(x, y - 1);
            Push(x, y + 1);
        }

        var seen = new bool[width * height];
        var centers = new List<double>();
        for (var index = 0; index < blocked.Length; index++)
        {
            if (blocked[index] || outside[index] || seen[index])
            {
                continue;
            }

            var count = 0;
            var sum = 0;
            var fill = new Queue<(int X, int Y)>();
            var startX = index % width;
            var startY = index / width;
            seen[index] = true;
            fill.Enqueue((startX, startY));
            while (fill.Count > 0)
            {
                var (x, y) = fill.Dequeue();
                count++;
                sum += y;
                void Take(int nx, int ny)
                {
                    if ((uint)nx >= (uint)width || (uint)ny >= (uint)height)
                    {
                        return;
                    }

                    var next = ny * width + nx;
                    if (blocked[next] || outside[next] || seen[next])
                    {
                        return;
                    }

                    seen[next] = true;
                    fill.Enqueue((nx, ny));
                }

                Take(x - 1, y);
                Take(x + 1, y);
                Take(x, y - 1);
                Take(x, y + 1);
            }

            if (count >= 2)
            {
                centers.Add((sum / (double)count) - 1);
            }
        }

        return centers;
    }

    private readonly struct Glyph
    {
        public Glyph(bool[] ink, int imageWidth, int left, int right, int top, int bottom, int bandTop, int bandBottom, int count)
        {
            InkSource = ink;
            ImageWidth = imageWidth;
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
            BandTop = bandTop;
            BandBottom = bandBottom;
            Ink = count;
        }

        public bool[] InkSource { get; }

        public int ImageWidth { get; }

        public int Left { get; }

        public int Right { get; }

        public int Top { get; }

        public int Bottom { get; }

        public int BandTop { get; }

        public int BandBottom { get; }

        public int Ink { get; }

        public int Width => Right - Left + 1;

        public int Height => Bottom - Top + 1;

        public bool IsInk(int x, int y) => InkSource[y * ImageWidth + x];
    }

    private static bool TryCrop(bool[] ink, int width, int left, int right, int bandTop, int bandBottom, out Glyph glyph)
    {
        var top = bandBottom;
        var bottom = bandTop;
        var count = 0;
        for (var y = bandTop; y <= bandBottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                if (!ink[y * width + x])
                {
                    continue;
                }

                count++;
                if (y < top)
                {
                    top = y;
                }

                if (y > bottom)
                {
                    bottom = y;
                }
            }
        }

        glyph = new Glyph(ink, width, left, right, top, bottom, bandTop, bandBottom, count);
        return count > 0;
    }

    private static bool TryInkBounds(
        bool[] ink,
        int width,
        int bandTop,
        int bandBottom,
        out int left,
        out int right,
        out int top,
        out int bottom,
        out int count)
    {
        left = width;
        right = 0;
        top = bandBottom;
        bottom = bandTop;
        count = 0;
        for (var y = bandTop; y <= bandBottom; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (!ink[y * width + x])
                {
                    continue;
                }

                count++;
                if (x < left)
                {
                    left = x;
                }

                if (x > right)
                {
                    right = x;
                }

                if (y < top)
                {
                    top = y;
                }

                if (y > bottom)
                {
                    bottom = y;
                }
            }
        }

        return count > 0;
    }

    private static bool ColumnHasInk(bool[] ink, int width, int x, int top, int bottom)
    {
        for (var y = top; y <= bottom; y++)
        {
            if (ink[y * width + x])
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<(int Top, int Bottom)> Bands(bool[] ink, int width, int height)
    {
        var start = -1;
        var gap = 0;
        for (var y = 0; y <= height; y++)
        {
            var row = false;
            if (y < height)
            {
                for (var x = 0; x < width && !row; x++)
                {
                    row = ink[y * width + x];
                }
            }

            if (row)
            {
                if (start < 0)
                {
                    start = y;
                }

                gap = 0;
            }
            else if (start >= 0)
            {
                gap++;
                if (gap > 1 || y == height)
                {
                    yield return (start, y - gap);
                    start = -1;
                    gap = 0;
                }
            }
        }
    }
}
