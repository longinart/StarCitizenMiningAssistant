namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// Reads the mining signature in a capture of the radar patch.
/// C# port of Smart Citizen src/utils/sco_digit_reader.py
/// (Apache-2.0, Osiris-DevWorks/smart-citizen). See NOTICE.
/// </summary>
public static class HudSignatureReader
{
    public readonly record struct Hit(string Text, int X, int Y, int Width, int Height, double Score, double FirstScore);

    private const int GridW = 4;
    private const int GridH = 7;
    private const int AltCount = 3;
    private const double AltMargin = 0.3;
    private const double MinReadingScore = 0.68;
    private const double ConfidentReadingScore = 0.80;
    private const double MinUndecodedScore = 0.85;
    private const int Fringe = 2;
    private const double WhiteLevel = 0.7;
    private const double MinWhiteFraction = 0.45;
    private const double MaxJoinedWidth = 2.6;
    private const double MinPartWidth = 0.2;
    private static readonly int[] Thresholds = [180, 200, 210, 150];
    private static readonly Lazy<(string Digit, double[] Features)[]> References = new(BuildReferences);

    public static Hit? ReadBmp(ReadOnlySpan<byte> bmp, double digitHeight, ScanSignatureChart chart)
    {
        if (!TryChannels(bmp, out var red, out var green, out var blue, out var width, out var height))
        {
            return null;
        }

        return Read(width, height, red, green, blue, digitHeight, chart);
    }

    public static Hit? Read(
        int width,
        int height,
        ReadOnlySpan<byte> red,
        ReadOnlySpan<byte> green,
        ReadOnlySpan<byte> blue,
        double digitHeight,
        ScanSignatureChart chart,
        bool allowSlant = true)
    {
        ArgumentNullException.ThrowIfNull(chart);
        if (width <= 0 || height <= 0 || red.Length != width * height || green.Length != red.Length || blue.Length != red.Length)
        {
            return null;
        }

        var redBytes = red.ToArray();
        var greenBytes = green.ToArray();
        var blueBytes = blue.ToArray();
        var maxBytes = new byte[redBytes.Length];
        for (var i = 0; i < maxBytes.Length; i++)
        {
            maxBytes[i] = Math.Max(redBytes[i], Math.Max(greenBytes[i], blueBytes[i]));
        }

        // Red is tried first: on a white digit it holds the shape. Green, blue, and the
        // strongest channel cover captures where chromatic fringing leaves the red edge
        // too thin to match a glyph. A row still has to be white on that channel.
        // A known number at 0.80 or above stops the search. When two readings are
        // that strong, the longer one is kept, so 45,000 is not reported as 4,000.
        // A reading whose first digit matches poorly is dropped, so a broken 6 is
        // not turned into 8,000. A weaker known number is kept only when nothing
        // stronger appears.
        (byte[] Ink, byte[] OtherA, byte[] OtherB)[] channels =
        [
            (redBytes, greenBytes, blueBytes),
            (greenBytes, redBytes, blueBytes),
            (blueBytes, redBytes, greenBytes),
            (maxBytes, greenBytes, blueBytes),
        ];
        var maxRun = (int)(1.2 * digitHeight);
        Hit? undecoded = null;
        Hit? weak = null;
        foreach (var threshold in Thresholds)
        {
            var decoded = new List<Hit>();
            var readings = new List<Hit>();
            foreach (var channel in channels)
            {
                var mask = Mask(width, height, channel.Ink, threshold, maxRun);
                var rows = CandidateRows(width, height, mask, digitHeight)
                    .Where(row => IsWhite(width, channel.Ink, channel.OtherA, channel.OtherB, row, threshold))
                    .ToList();
                foreach (var hit in rows.Select(row => ReadRow(width, channel.Ink, row, threshold, chart)).OfType<Hit>())
                {
                    if (hit.FirstScore < 0.80)
                    {
                        continue;
                    }

                    readings.Add(hit);
                    if (chart.IsKnown(Digits(hit.Text)))
                    {
                        decoded.Add(hit);
                    }
                }
            }

            var confident = readings.Where(hit => hit.Score >= ConfidentReadingScore).ToList();
            if (confident.Count > 0)
            {
                return confident
                    .OrderByDescending(hit => Digits(hit.Text).ToString(System.Globalization.CultureInfo.InvariantCulture).Length)
                    .ThenByDescending(hit => hit.Score)
                    .First();
            }

            if (decoded.Count > 0)
            {
                var best = decoded.MaxBy(hit => hit.Score);
                if (weak is null || best.Score > weak.Value.Score)
                {
                    weak = best;
                }
            }

            if (readings.Count > 0 && undecoded is null)
            {
                undecoded = readings.MaxBy(hit => hit.Score);
            }
        }

        if (weak is not null && (undecoded is null || weak.Value.Score >= undecoded.Value.Score))
        {
            return weak;
        }

        if (undecoded is not null || !allowSlant)
        {
            return undecoded;
        }

        return ReadSlanted(width, height, redBytes, greenBytes, blueBytes, maxBytes, digitHeight, chart);
    }

    private static Hit? ReadSlanted(
        int width,
        int height,
        byte[] red,
        byte[] green,
        byte[] blue,
        byte[] max,
        double digitHeight,
        ScanSignatureChart chart)
    {
        var pill = FindPill(width, height, red, green, blue, digitHeight);
        if (pill is not null)
        {
            var turned = TurnAndRead(width, height, red, green, blue, digitHeight, chart, pill.Value, smooth: true);
            if (turned is not null)
            {
                return turned;
            }
        }

        var fromGreen = FindSlant(width, height, green, digitHeight);
        var fromMax = FindSlant(width, height, max, digitHeight);
        var slant = fromGreen is null ? fromMax
            : fromMax is null || fromGreen.Value.Count >= fromMax.Value.Count ? fromGreen : fromMax;
        if (slant is null)
        {
            return null;
        }

        return TurnAndRead(width, height, red, green, blue, digitHeight, chart, slant.Value, smooth: false);
    }

    private static Hit? TurnAndRead(
        int width,
        int height,
        byte[] red,
        byte[] green,
        byte[] blue,
        double digitHeight,
        ScanSignatureChart chart,
        Slant slant,
        bool smooth)
    {
        var turnedRed = new byte[red.Length];
        var turnedGreen = new byte[green.Length];
        var turnedBlue = new byte[blue.Length];
        Rotate(width, height, red, turnedRed, -slant.Angle, slant.X, slant.Y, smooth);
        Rotate(width, height, green, turnedGreen, -slant.Angle, slant.X, slant.Y, smooth);
        Rotate(width, height, blue, turnedBlue, -slant.Angle, slant.X, slant.Y, smooth);
        return Read(width, height, turnedRed, turnedGreen, turnedBlue, digitHeight, chart, allowSlant: false);
    }

    private static Slant? FindPill(int width, int height, byte[] red, byte[] green, byte[] blue, double digitHeight)
    {
        var mask = new byte[red.Length];
        for (var i = 0; i < mask.Length; i++)
        {
            var r = red[i];
            var g = green[i];
            var b = blue[i];
            if (r >= 28 && r <= 110 && g <= 85 && b <= 60 && r >= b + 6 && r + 12 >= g)
            {
                mask[i] = 1;
            }
        }

        Slant? best = null;
        foreach (var box in Components(width, height, mask))
        {
            if (box.W < 4 * digitHeight || box.W > 16 * digitHeight
                || box.H < 1.4 * digitHeight || box.H > 6 * digitHeight
                || box.W < box.H
                || PinPixels(width, height, red, green, blue, box, digitHeight) < 12)
            {
                continue;
            }

            var angle = PillEdge(width, mask, box);
            if (angle is null || !MildTilt(angle.Value) || (best is not null && box.W * box.H <= best.Value.Count))
            {
                continue;
            }

            best = new Slant(angle.Value, box.X + (box.W / 2), box.Y + (box.H / 2), box.W * box.H);
        }

        return best;
    }

    private static int PinPixels(
        int width,
        int height,
        byte[] red,
        byte[] green,
        byte[] blue,
        (int X, int Y, int W, int H) box,
        double digitHeight)
    {
        var left = Math.Max(0, box.X - (int)digitHeight);
        var right = Math.Min(width - 1, box.X + (int)(box.W * 0.45));
        var top = Math.Max(0, box.Y);
        var bottom = Math.Min(height - 1, box.Y + box.H - 1);
        var count = 0;
        for (var y = top; y <= bottom; y++)
        {
            var row = y * width;
            for (var x = left; x <= right; x++)
            {
                var r = red[row + x];
                var g = green[row + x];
                var b = blue[row + x];
                if (r >= 90 && r >= g + 25 && r >= b + 20 && b <= 100)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static double? PillEdge(int width, byte[] mask, (int X, int Y, int W, int H) box)
    {
        var xs = new List<double>();
        var ys = new List<double>();
        var yEnd = Math.Min(box.Y + box.H, mask.Length / width);
        var xEnd = Math.Min(box.X + box.W, width);
        for (var x = Math.Max(box.X, 0); x < xEnd; x += 3)
        {
            for (var y = Math.Max(box.Y, 0); y < yEnd; y++)
            {
                if (mask[(y * width) + x] == 0)
                {
                    continue;
                }

                xs.Add(x);
                ys.Add(y);
                break;
            }
        }

        if (xs.Count < 6)
        {
            return null;
        }

        var fitted = Fit(xs, ys);
        return fitted is null ? null : Math.Atan(fitted.Value.Slope);
    }

    private static bool MildTilt(double radians)
    {
        var degrees = Math.Abs(radians) * 180 / Math.PI;
        return degrees is >= 3 and <= 35;
    }

    private readonly record struct Slant(double Angle, int X, int Y, int Count);

    private static Slant? FindSlant(int width, int height, byte[] green, double digitHeight)
    {
        var mask = Mask(width, height, green, 180, width);
        var boxes = Components(width, height, mask);
        var chain = SlantFromDigits(boxes, digitHeight);
        var row = SlantFromRow(width, mask, boxes, digitHeight);
        if (chain is null)
        {
            return row;
        }

        if (row is null || chain.Value.Count >= row.Value.Count)
        {
            return chain;
        }

        return row;
    }

    private static Slant? SlantFromDigits(List<(int X, int Y, int W, int H)> boxes, double digitHeight)
    {
        var digits = boxes
            .Where(box => box.W >= 0.25 * digitHeight && box.H >= 0.25 * digitHeight
                && box.W <= 1.8 * digitHeight && box.H <= 1.8 * digitHeight)
            .OrderBy(box => box.X)
            .ToList();
        Slant? best = null;
        for (var i = 0; i < digits.Count; i++)
        {
            var chain = new List<(int X, int Y, int W, int H)> { digits[i] };
            for (var j = i + 1; j < digits.Count; j++)
            {
                var next = digits[j];
                var last = chain[^1];
                var gap = next.X - (last.X + last.W);
                if (gap > 2.2 * digitHeight)
                {
                    break;
                }

                if (gap < -0.4 * digitHeight)
                {
                    continue;
                }

                var cx = next.X + (next.W / 2.0);
                var cy = next.Y + (next.H / 2.0);
                if (chain.Count == 1)
                {
                    var dy = Math.Abs(cy - (last.Y + (last.H / 2.0)));
                    if (dy > 1.6 * digitHeight)
                    {
                        continue;
                    }
                }
                else if (Distance(chain[0], chain[^1], cx, cy) > 0.55 * digitHeight)
                {
                    continue;
                }

                chain.Add(next);
            }

            if (chain.Count < 4)
            {
                continue;
            }

            var first = chain[0];
            var end = chain[^1];
            var dx = (end.X + (end.W / 2.0)) - (first.X + (first.W / 2.0));
            var rise = (end.Y + (end.H / 2.0)) - (first.Y + (first.H / 2.0));
            if (Math.Abs(dx) < digitHeight || !AcceptedTilt(Math.Atan2(rise, dx)))
            {
                continue;
            }

            var tight = chain.All(box => Distance(first, end, box.X + (box.W / 2.0), box.Y + (box.H / 2.0)) <= 0.55 * digitHeight);
            var ink = chain.Sum(box => box.W * box.H);
            if (!tight || (best is not null && ink <= best.Value.Count))
            {
                continue;
            }

            best = new Slant(
                Math.Atan2(rise, dx),
                (int)(first.X + ((end.X + end.W - first.X) / 2.0)),
                (int)(first.Y + (first.H / 2.0) + (rise / 2.0)),
                ink);
        }

        return best;
    }

    private static Slant? SlantFromRow(int width, byte[] mask, List<(int X, int Y, int W, int H)> boxes, double digitHeight)
    {
        Slant? best = null;
        foreach (var box in boxes)
        {
            if (box.W < 2.0 * digitHeight || box.W > 8.0 * digitHeight || box.H < 0.6 * digitHeight || box.H > 3.5 * digitHeight)
            {
                continue;
            }

            long sumX = 0;
            long sumY = 0;
            long sumXX = 0;
            long sumYY = 0;
            long sumXY = 0;
            var count = 0;
            var minX = int.MaxValue;
            var maxX = int.MinValue;
            var minY = int.MaxValue;
            var maxY = int.MinValue;
            var yEnd = Math.Min(box.Y + box.H, mask.Length / width);
            var xEnd = Math.Min(box.X + box.W, width);
            for (var y = Math.Max(box.Y, 0); y < yEnd; y++)
            {
                var row = y * width;
                for (var x = Math.Max(box.X, 0); x < xEnd; x++)
                {
                    if (mask[row + x] == 0)
                    {
                        continue;
                    }

                    count++;
                    sumX += x;
                    sumY += y;
                    sumXX += (long)x * x;
                    sumYY += (long)y * y;
                    sumXY += (long)x * y;
                    if (x < minX)
                    {
                        minX = x;
                    }

                    if (x > maxX)
                    {
                        maxX = x;
                    }

                    if (y < minY)
                    {
                        minY = y;
                    }

                    if (y > maxY)
                    {
                        maxY = y;
                    }
                }
            }

            if (count < 24)
            {
                continue;
            }

            var cx = sumX / (double)count;
            var cy = sumY / (double)count;
            var cxx = (sumXX / (double)count) - (cx * cx);
            var cyy = (sumYY / (double)count) - (cy * cy);
            var cxy = (sumXY / (double)count) - (cx * cy);
            var spread = Math.Sqrt(((cxx - cyy) * (cxx - cyy)) + (4 * cxy * cxy));
            var major = 0.5 * (cxx + cyy + spread);
            var minor = 0.5 * (cxx + cyy - spread);
            if (major < 2.5 * Math.Max(minor, 0.01) || Math.Sqrt(Math.Max(minor, 0)) > 0.9 * digitHeight)
            {
                continue;
            }

            var angle = Midline(width, mask, box, digitHeight) ?? (0.5 * Math.Atan2(2 * cxy, cxx - cyy));
            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);
            foreach (var other in boxes)
            {
                var otherX = other.X + (other.W / 2.0);
                var otherY = other.Y + (other.H / 2.0);
                if (otherX < minX - (1.6 * digitHeight) || otherX > maxX + (1.6 * digitHeight))
                {
                    continue;
                }

                if (other.W > 2.2 * digitHeight || other.H > 2.2 * digitHeight)
                {
                    continue;
                }

                var along = ((otherX - cx) * cos) + ((otherY - cy) * sin);
                var across = Math.Abs((-(otherX - cx) * sin) + ((otherY - cy) * cos));
                if (Math.Abs(along) > 6 * digitHeight || across > 0.8 * digitHeight)
                {
                    continue;
                }

                minX = Math.Min(minX, other.X);
                maxX = Math.Max(maxX, other.X + other.W - 1);
                minY = Math.Min(minY, other.Y);
                maxY = Math.Max(maxY, other.Y + other.H - 1);
            }

            var pivotX = (minX + maxX) / 2.0;
            var pivotY = (minY + maxY) / 2.0;
            if (!AcceptedTilt(angle) || (best is not null && count <= best.Value.Count))
            {
                continue;
            }

            best = new Slant(angle, (int)Math.Round(pivotX), (int)Math.Round(pivotY), count);
        }

        return best;
    }

    private static double? Midline(int width, byte[] mask, (int X, int Y, int W, int H) box, double digitHeight)
    {
        var slice = Math.Max(2, (int)Math.Round(digitHeight * 0.55));
        var yEnd = Math.Min(box.Y + box.H, mask.Length / width);
        var xEnd = Math.Min(box.X + box.W, width);
        var xs = new List<double>();
        var ys = new List<double>();
        for (var x0 = Math.Max(box.X, 0); x0 < xEnd; x0 += slice)
        {
            long sumX = 0;
            var count = 0;
            var top = int.MaxValue;
            var stop = Math.Min(x0 + slice, xEnd);
            for (var y = Math.Max(box.Y, 0); y < yEnd; y++)
            {
                var row = y * width;
                for (var x = x0; x < stop; x++)
                {
                    if (mask[row + x] == 0)
                    {
                        continue;
                    }

                    count++;
                    sumX += x;
                    if (y < top)
                    {
                        top = y;
                    }
                }
            }

            if (count < 6)
            {
                continue;
            }

            xs.Add(sumX / (double)count);
            ys.Add(top);
        }

        if (xs.Count < 3)
        {
            return null;
        }

        var fitted = Fit(xs, ys);
        return fitted is null ? null : Math.Atan(fitted.Value.Slope);
    }

    private readonly record struct FitLine(double Slope, double Intercept);

    private static FitLine? Fit(List<double> xs, List<double> ys)
    {
        double sumX = 0;
        double sumY = 0;
        double sumXX = 0;
        double sumXY = 0;
        for (var i = 0; i < xs.Count; i++)
        {
            sumX += xs[i];
            sumY += ys[i];
            sumXX += xs[i] * xs[i];
            sumXY += xs[i] * ys[i];
        }

        var n = xs.Count;
        var denom = (n * sumXX) - (sumX * sumX);
        if (Math.Abs(denom) < 1)
        {
            return null;
        }

        var slope = ((n * sumXY) - (sumX * sumY)) / denom;
        return new FitLine(slope, (sumY - (slope * sumX)) / n);
    }

    private static bool AcceptedTilt(double radians)
    {
        var degrees = Math.Abs(radians) * 180 / Math.PI;
        return degrees is >= 6 and <= 35;
    }

    private static double Distance((int X, int Y, int W, int H) first, (int X, int Y, int W, int H) end, double px, double py)
    {
        var x1 = first.X + (first.W / 2.0);
        var y1 = first.Y + (first.H / 2.0);
        var x2 = end.X + (end.W / 2.0);
        var y2 = end.Y + (end.H / 2.0);
        var length = Math.Sqrt(((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1)));
        if (length < 1)
        {
            return double.MaxValue;
        }

        return Math.Abs(((y2 - y1) * px) - ((x2 - x1) * py) + (x2 * y1) - (y2 * x1)) / length;
    }

    private static void Rotate(int width, int height, byte[] source, byte[] destination, double radians, int originX, int originY, bool smooth)
    {
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var dx = x - originX;
                var dy = y - originY;
                var sx = originX + (cos * dx) + (sin * dy);
                var sy = originY - (sin * dx) + (cos * dy);
                var index = (y * width) + x;
                destination[index] = smooth
                    ? Sample(source, width, height, sx, sy)
                    : Nearest(source, width, height, sx, sy);
            }
        }
    }

    private static byte Nearest(byte[] source, int width, int height, double x, double y)
    {
        var sx = (int)Math.Round(x);
        var sy = (int)Math.Round(y);
        return sx >= 0 && sy >= 0 && sx < width && sy < height ? source[(sy * width) + sx] : (byte)0;
    }

    private static byte Sample(byte[] source, int width, int height, double x, double y)
    {
        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        if (x0 < 0 || y0 < 0 || x0 + 1 >= width || y0 + 1 >= height)
        {
            return 0;
        }

        var tx = x - x0;
        var ty = y - y0;
        var index = (y0 * width) + x0;
        var top = (source[index] * (1 - tx)) + (source[index + 1] * tx);
        var bottom = (source[index + width] * (1 - tx)) + (source[index + width + 1] * tx);
        return (byte)Math.Clamp((top * (1 - ty)) + (bottom * ty), 0, 255);
    }

    private static int Digits(string text) => int.Parse(text.Replace(",", "", StringComparison.Ordinal), System.Globalization.CultureInfo.InvariantCulture);

    private static List<(int X, int Y, int W, int H)> Components(int width, int height, byte[] mask)
    {
        var parent = new List<int>();
        var bbox = new List<int[]>();
        var above = new List<(int X0, int X1, int Id)>();
        for (var y = 0; y < height; y++)
        {
            var row = y * width;
            var current = new List<(int X0, int X1, int Id)>();
            var k = 0;
            var start = 0;
            while (start < width)
            {
                while (start < width && mask[row + start] == 0)
                {
                    start++;
                }

                if (start >= width)
                {
                    break;
                }

                var end = start;
                while (end < width && mask[row + end] != 0)
                {
                    end++;
                }

                var x0 = start;
                var x1 = end - 1;
                var id = parent.Count;
                parent.Add(id);
                var box = new[] { x0, y, x1, y };
                bbox.Add(box);
                while (k < above.Count && above[k].X1 < x0 - 1)
                {
                    k++;
                }

                var j = k;
                var root = id;
                while (j < above.Count && above[j].X0 <= x1 + 1)
                {
                    var other = Find(parent, above[j].Id);
                    if (other != root)
                    {
                        parent[root] = other;
                        var kept = bbox[other];
                        kept[0] = Math.Min(kept[0], box[0]);
                        kept[1] = Math.Min(kept[1], box[1]);
                        kept[2] = Math.Max(kept[2], box[2]);
                        kept[3] = Math.Max(kept[3], box[3]);
                        root = other;
                        box = kept;
                    }

                    j++;
                }

                current.Add((x0, x1, id));
                start = end;
            }

            above = current;
        }

        var boxes = new List<(int X, int Y, int W, int H)>();
        for (var i = 0; i < parent.Count; i++)
        {
            if (parent[i] != i)
            {
                continue;
            }

            var box = bbox[i];
            boxes.Add((box[0], box[1], box[2] - box[0] + 1, box[3] - box[1] + 1));
        }

        return boxes;
    }

    private static int Find(List<int> parent, int index)
    {
        while (parent[index] != index)
        {
            parent[index] = parent[parent[index]];
            index = parent[index];
        }

        return index;
    }

    private static byte[] Mask(int width, int height, byte[] red, int threshold, int maxRun)
    {
        var mask = new byte[red.Length];
        for (var i = 0; i < red.Length; i++)
        {
            mask[i] = red[i] >= threshold ? (byte)1 : (byte)0;
        }

        for (var row = 0; row < width * height; row += width)
        {
            var x = 0;
            while (x < width)
            {
                while (x < width && mask[row + x] == 0)
                {
                    x++;
                }

                var start = x;
                while (x < width && mask[row + x] != 0)
                {
                    x++;
                }

                if (x - start > maxRun)
                {
                    Array.Clear(mask, row + start, x - start);
                }
            }
        }

        return mask;
    }

    private static bool DigitTall((int X, int Y, int W, int H) box, double digitHeight) =>
        0.75 * digitHeight <= box.H && box.H <= 1.35 * digitHeight;

    private static List<(int X, int Y, int W, int H)> SplitJoined(int width, byte[] mask, (int X, int Y, int W, int H) box, double digitHeight)
    {
        if (box.W <= digitHeight)
        {
            return [box];
        }

        var minPart = Math.Max(2, (int)Math.Round(MinPartWidth * digitHeight, MidpointRounding.ToEven));
        if (box.W > MaxJoinedWidth * digitHeight || box.W < (2 * minPart) + 1)
        {
            return [];
        }

        var ink = new int[box.W];
        for (var x = 0; x < box.W; x++)
        {
            for (var y = box.Y; y < box.Y + box.H; y++)
            {
                ink[x] += mask[(y * width) + box.X + x];
            }
        }

        var cut = minPart;
        var bestInk = int.MaxValue;
        var bestTie = int.MaxValue;
        for (var i = minPart; i < box.W - minPart; i++)
        {
            var tie = Math.Abs((2 * i) + 1 - box.W);
            if (ink[i] < bestInk || (ink[i] == bestInk && tie < bestTie))
            {
                bestInk = ink[i];
                bestTie = tie;
                cut = i;
            }
        }

        var parts = new List<(int X, int Y, int W, int H)>();
        foreach (var (start, end) in new[] { (box.X, box.X + cut), (box.X + cut + 1, box.X + box.W) })
        {
            var rows = new List<int>();
            for (var y = box.Y; y < box.Y + box.H; y++)
            {
                for (var x = start; x < end; x++)
                {
                    if (mask[(y * width) + x] != 0)
                    {
                        rows.Add(y);
                        break;
                    }
                }
            }

            if (rows.Count > 0)
            {
                var part = (start, rows[0], end - start, rows[^1] - rows[0] + 1);
                parts.AddRange(SplitJoined(width, mask, part, digitHeight));
            }
        }

        return parts;
    }

    private static List<((int X, int Y, int W, int H)[] Boxes, int Top, int Base)> CandidateRows(int width, int height, byte[] mask, double digitHeight)
    {
        var boxes = Components(width, height, mask);
        var digits = boxes
            .Where(box => DigitTall(box, digitHeight))
            .SelectMany(box => SplitJoined(width, mask, box, digitHeight))
            .Where(part => DigitTall(part, digitHeight))
            .OrderBy(part => part.X)
            .ToList();
        var commas = boxes.Where(box => box.H <= 0.6 * digitHeight && box.W <= 0.5 * digitHeight).ToList();
        var rows = new List<((int X, int Y, int W, int H)[] Boxes, int Top, int Base)>();
        var used = new HashSet<int>();
        for (var i = 0; i < digits.Count; i++)
        {
            if (!used.Add(i))
            {
                continue;
            }

            var chain = new List<(int X, int Y, int W, int H)> { digits[i] };
            for (var j = i + 1; j < digits.Count; j++)
            {
                if (used.Contains(j))
                {
                    continue;
                }

                var next = digits[j];
                var last = chain[^1];
                var gap = next.X - (last.X + last.W);
                if (gap > 1.6 * digitHeight)
                {
                    if (next.X > last.X + (3 * digitHeight))
                    {
                        break;
                    }

                    continue;
                }

                if (gap >= -1
                    && Math.Abs(next.Y - last.Y) <= 2
                    && Math.Abs(next.Y + next.H - last.Y - last.H) <= 2)
                {
                    chain.Add(next);
                    used.Add(j);
                }
            }

            if (chain.Count < 4)
            {
                continue;
            }

            var bottoms = chain.Select(box => box.Y + box.H).Order().ToArray();
            var baseline = bottoms[bottoms.Length / 2];
            var top = chain.Min(box => box.Y);
            for (var k = 0; k < chain.Count - 1; k++)
            {
                var left = chain[k];
                var right = chain[k + 1];
                var hasComma = commas.Any(comma =>
                    left.X + left.W - 1 <= comma.X
                    && comma.X + comma.W <= right.X + 1
                    && comma.Y + comma.H >= baseline - 2
                    && comma.Y >= top + (0.5 * digitHeight));
                if (hasComma && k + 1 is >= 1 and <= 2 && chain.Count - (k + 1) >= 3)
                {
                    rows.Add((chain.Take(k + 4).ToArray(), top, baseline));
                    break;
                }
            }
        }

        return rows;
    }

    private static Hit? ReadRow(int width, byte[] red, ((int X, int Y, int W, int H)[] Boxes, int Top, int Base) row, int threshold, ScanSignatureChart chart)
    {
        var options = new List<List<(double Score, string Digit)>>();
        foreach (var box in row.Boxes)
        {
            var pixels = GlyphPixels(width, red, box, row.Top, row.Base, threshold);
            if (pixels is null)
            {
                return null;
            }

            var ranked = Rank(pixels);
            var kept = new List<(double Score, string Digit)> { ranked[0] };
            foreach (var alt in ranked.Skip(1).Take(AltCount - 1))
            {
                if (ranked[0].Score - alt.Score <= AltMargin)
                {
                    kept.Add(alt);
                }
            }

            options.Add(kept);
        }

        (double Score, string Text, double Min, double First)? bestAny = null;
        (double Score, string Text, double First)? bestDecoded = null;
        var index = new int[options.Count];
        while (true)
        {
            if (options[0][index[0]].Digit != "0")
            {
                var text = string.Concat(options.Select((option, i) => option[index[i]].Digit));
                var scores = options.Select((option, i) => option[index[i]].Score).ToArray();
                var score = scores.Average();
                var min = scores.Min();
                if (bestAny is null || score > bestAny.Value.Score)
                {
                    bestAny = (score, text, min, scores[0]);
                }

                if (int.TryParse(text, out var value) && chart.IsKnown(value) && (bestDecoded is null || score > bestDecoded.Value.Score))
                {
                    bestDecoded = (score, text, scores[0]);
                }
            }

            var cursor = index.Length - 1;
            while (cursor >= 0)
            {
                index[cursor]++;
                if (index[cursor] < options[cursor].Count)
                {
                    break;
                }

                index[cursor] = 0;
                cursor--;
            }

            if (cursor < 0)
            {
                break;
            }
        }

        string chosen;
        double chosenScore;
        double chosenFirst;
        if (bestDecoded is not null && bestDecoded.Value.Score >= ConfidentReadingScore)
        {
            chosen = bestDecoded.Value.Text;
            chosenScore = bestDecoded.Value.Score;
            chosenFirst = bestDecoded.Value.First;
        }
        else if (bestAny is not null && bestAny.Value.Min >= MinUndecodedScore)
        {
            chosen = bestAny.Value.Text;
            chosenScore = bestAny.Value.Score;
            chosenFirst = bestAny.Value.First;
        }
        else if (bestDecoded is not null && bestDecoded.Value.Score >= MinReadingScore)
        {
            chosen = bestDecoded.Value.Text;
            chosenScore = bestDecoded.Value.Score;
            chosenFirst = bestDecoded.Value.First;
        }
        else
        {
            return null;
        }

        var x0 = row.Boxes[0].X;
        var x1 = row.Boxes.Max(box => box.X + box.W);
        var split = chosen.Length - 3;
        return new Hit(
            chosen[..split] + "," + chosen[split..],
            x0,
            row.Top,
            x1 - x0,
            row.Base - row.Top,
            chosenScore,
            chosenFirst);
    }

    private static bool IsWhite(int width, byte[] red, byte[] green, byte[] blue, ((int X, int Y, int W, int H)[] Boxes, int Top, int Base) row, int threshold)
    {
        var x0 = row.Boxes[0].X;
        var x1 = row.Boxes.Max(box => box.X + box.W);
        var inked = 0;
        var white = 0;
        for (var y = row.Top; y < row.Base; y++)
        {
            var line = y * width;
            for (var x = x0; x < x1; x++)
            {
                var value = red[line + x];
                if (value < threshold)
                {
                    continue;
                }

                inked++;
                var lo = Math.Max(0, x - Fringe);
                var hi = Math.Min(width, x + Fringe + 1);
                var need = WhiteLevel * value;
                var greenMax = 0;
                var blueMax = 0;
                for (var i = lo; i < hi; i++)
                {
                    greenMax = Math.Max(greenMax, green[line + i]);
                    blueMax = Math.Max(blueMax, blue[line + i]);
                }

                if (greenMax >= need && blueMax >= need)
                {
                    white++;
                }
            }
        }

        return inked > 0 && white >= MinWhiteFraction * inked;
    }

    private static int[][]? GlyphPixels(int width, byte[] red, (int X, int Y, int W, int H) box, int top, int baseline, int threshold)
    {
        var cols = new List<int>();
        for (var x = box.X; x < box.X + box.W; x++)
        {
            for (var y = top; y < baseline; y++)
            {
                if (red[(y * width) + x] >= threshold)
                {
                    cols.Add(x);
                    break;
                }
            }
        }

        if (cols.Count == 0)
        {
            return null;
        }

        var pixels = new int[baseline - top][];
        for (var y = top; y < baseline; y++)
        {
            var line = new int[cols[^1] - cols[0] + 1];
            for (var x = cols[0]; x <= cols[^1]; x++)
            {
                line[x - cols[0]] = red[(y * width) + x];
            }

            pixels[y - top] = line;
        }

        return pixels;
    }

    private static List<(double Score, string Digit)> Rank(int[][] pixels)
    {
        var feat = Features(pixels);
        var best = new Dictionary<string, double>();
        foreach (var (digit, reference) in References.Value)
        {
            var score = 0.0;
            for (var i = 0; i < feat.Length; i++)
            {
                score += feat[i] * reference[i];
            }

            if (!best.TryGetValue(digit, out var prior) || score > prior)
            {
                best[digit] = score;
            }
        }

        return best.Select(pair => (pair.Value, pair.Key)).OrderByDescending(pair => pair.Value).ToList();
    }

    private static (string Digit, double[] Features)[] BuildReferences()
    {
        var refs = new List<(string, double[])>();
        foreach (var sample in HudSignatureGlyphs.Samples)
        {
            var raw = Convert.FromHexString(sample.Hex);
            var pixels = new int[sample.Height][];
            for (var y = 0; y < sample.Height; y++)
            {
                var line = new int[sample.Width];
                for (var x = 0; x < sample.Width; x++)
                {
                    line[x] = raw[(y * sample.Width) + x];
                }

                pixels[y] = line;
            }

            refs.Add((sample.Digit, Features(pixels)));
        }

        return refs.ToArray();
    }

    private static double[] Features(int[][] pixels)
    {
        var height = pixels.Length;
        var width = pixels[0].Length;
        var lo = pixels.Min(line => line.Min());
        var hi = pixels.Max(line => line.Max());
        var output = new double[GridW * GridH];
        var index = 0;
        for (var gy = 0; gy < GridH; gy++)
        {
            var y0 = gy * height / (double)GridH;
            var y1 = (gy + 1) * height / (double)GridH;
            for (var gx = 0; gx < GridW; gx++)
            {
                var x0 = gx * width / (double)GridW;
                var x1 = (gx + 1) * width / (double)GridW;
                var total = 0.0;
                var area = 0.0;
                for (var y = (int)y0; y < Math.Min(height, (int)(y1 + 0.999)); y++)
                {
                    var wy = Math.Min(y + 1, y1) - Math.Max(y, y0);
                    for (var x = (int)x0; x < Math.Min(width, (int)(x1 + 0.999)); x++)
                    {
                        var wx = Math.Min(x + 1, x1) - Math.Max(x, x0);
                        total += wy * wx * pixels[y][x];
                        area += wy * wx;
                    }
                }

                output[index++] = (total / area - lo) / (hi - lo + 1e-9);
            }
        }

        var mean = output.Average();
        var norm = 0.0;
        for (var i = 0; i < output.Length; i++)
        {
            output[i] -= mean;
            norm += output[i] * output[i];
        }

        norm = Math.Sqrt(norm);
        if (norm == 0)
        {
            norm = 1;
        }

        for (var i = 0; i < output.Length; i++)
        {
            output[i] /= norm;
        }

        return output;
    }

    private static bool TryChannels(ReadOnlySpan<byte> image, out byte[] red, out byte[] green, out byte[] blue, out int width, out int height)
    {
        red = [];
        green = [];
        blue = [];
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

        red = new byte[width * height];
        green = new byte[width * height];
        blue = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            var fileRow = height - 1 - y;
            for (var x = 0; x < width; x++)
            {
                var index = pixelOffset + (fileRow * stride) + (x * 3);
                var pixel = (y * width) + x;
                blue[pixel] = image[index];
                green[pixel] = image[index + 1];
                red[pixel] = image[index + 2];
            }
        }

        return true;
    }
}
