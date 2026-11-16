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
        ScanSignatureChart chart)
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

        return undecoded;
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
