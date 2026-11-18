namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// Reads the pale-green numbers on the scan-results panel from the green channel.
/// The shapes were taken from cockpit screenshots of those digits, including 1, 6, 7 and 9.
/// A small low mark is a decimal. A wide mark after the digits is a percent sign and is not part of the number.
/// A digit that does not match drops the whole number, and a short fragment of that row is not reported.
/// A zero keeps the diagonal stroke that separates it from the letter O. An 8 has a bar with a hole above and below.
/// This set is not the signature glyph set.
/// </summary>
public static class RockDigitReader
{
    public readonly record struct Amount(string Text, int Y, int X, int Digits = 0, int Cutoff = 0);

    private const int GridW = 7;
    private const int GridH = 9;
    private static readonly (string Digit, bool[,] Grid)[] Templates = Build();

    public static IReadOnlyList<Amount> Read(int width, int height, ReadOnlySpan<byte> green)
    {
        if (width <= 0 || height <= 0 || green.Length != width * height)
        {
            return [];
        }

        var found = new List<Amount>();
        foreach (var cutoff in new[] { 190, 210, 230 })
        {
            found.AddRange(Rows(width, height, green, cutoff));
        }

        return found
            .GroupBy(amount => found.Where(other => Math.Abs(other.Y - amount.Y) <= 4).Min(other => other.Y))
            .Select(Choose)
            .OrderBy(amount => amount.Y)
            .ToList();
    }

    private static List<Amount> Rows(int width, int height, ReadOnlySpan<byte> green, int cutoff)
    {
        var ink = new bool[width * height];
        for (var i = 0; i < ink.Length; i++)
        {
            ink[i] = green[i] >= cutoff;
        }

        var rows = new List<Amount>();
        var y = 0;
        while (y < height)
        {
            if (Count(ink, width, y) < 4)
            {
                y++;
                continue;
            }

            var start = y;
            while (y < height && Count(ink, width, y) >= 4)
            {
                y++;
            }

            var rowHeight = y - start;
            if (rowHeight is < 6 or > 18)
            {
                continue;
            }

            if (ReadRow(ink, width, start, y, cutoff) is Amount amount)
            {
                rows.Add(amount);
            }
        }

        return rows;
    }

    private static int Count(bool[] ink, int width, int y)
    {
        var n = 0;
        var line = y * width;
        for (var x = 0; x < width; x++)
        {
            if (ink[line + x])
            {
                n++;
            }
        }

        return n;
    }

    private static Amount? ReadRow(bool[] ink, int width, int top, int bottom, int cutoff)
    {
        var glyphs = new List<string>();
        var x = 0;
        int? origin = null;
        int? first = null;
        var marks = 0;
        var pendingDot = false;
        var skippedDigit = false;
        while (x < width)
        {
            if (!Column(ink, width, x, top, bottom))
            {
                x++;
                continue;
            }

            var start = x;
            while (x < width && Column(ink, width, x, top, bottom))
            {
                x++;
            }

            var mark = Measure(ink, width, start, x, top, bottom);
            // A decimal is a small mark low between two digits. A narrow full-height stroke is a 1.
            if (mark.Height <= 4 && mark.Width <= 3)
            {
                if (origin is not null
                    && mark.Count >= 2
                    && mark.Center >= top + ((bottom - top) * 0.45))
                {
                    pendingDot = true;
                }

                continue;
            }

            if (mark.Count < 4)
            {
                continue;
            }

            var sized = mark.Width is >= 3 and <= 8 && mark.Height > 4;
            if (sized)
            {
                marks++;
                first ??= start;
            }

            // A percent sign is wider than a digit and is not part of the number.
            if (mark.Width >= 9)
            {
                if (origin is not null)
                {
                    break;
                }

                continue;
            }

            var crop = Crop(ink, width, start, x, top, bottom);
            var body = Tight(crop);
            var matched = Classify(crop, body);
            if (matched is null)
            {
                if (origin is null)
                {
                    if (sized)
                    {
                        skippedDigit = true;
                    }

                    continue;
                }

                return Incomplete(top, first, marks, cutoff);
            }

            if (skippedDigit)
            {
                return Incomplete(top, first, marks, cutoff);
            }

            if (pendingDot)
            {
                glyphs.Add(".");
                pendingDot = false;
            }

            glyphs.Add(matched);
            origin ??= start;
        }

        if (origin is not int left || glyphs.Count == 0 || glyphs[0] == "." || glyphs[^1] == ".")
        {
            return Incomplete(top, first, marks, cutoff);
        }

        var text = string.Concat(glyphs);
        return text.Count(character => character == '.') > 1
            ? Incomplete(top, first, marks, cutoff)
            : new Amount(text, top, left, text.Count(char.IsDigit), cutoff);
    }

    /// <summary>
    /// A row with several digit-sized marks and no complete reading. The panel leaves a shorter text blank.
    /// </summary>
    private static Amount? Incomplete(int top, int? first, int marks, int cutoff) =>
        marks >= 2 && first is int edge ? new Amount("", top, edge, marks, cutoff) : null;

    private static Amount Choose(IEnumerable<Amount> group)
    {
        var items = group.ToList();
        var complete = items
            .Where(amount => amount.Text.Length > 0)
            .OrderByDescending(amount => DigitCount(amount.Text))
            .ThenByDescending(amount => amount.Cutoff)
            .ThenByDescending(amount => amount.Text.Length)
            .FirstOrDefault();
        if (complete.Text is { Length: > 0 })
        {
            // A longer failure that starts on the same glyph means this reading is only a fragment.
            // Ink further left on the same line is a different mark and does not erase the number.
            var digits = DigitCount(complete.Text);
            // A stricter cutoff splits a stroke and can count one glyph twice. Only a looser cutoff
            // that saw more glyphs shows this reading is a fragment of a longer row.
            var fragment = items.Any(amount =>
                amount.Text.Length == 0
                && amount.Digits > digits
                && amount.Cutoff <= complete.Cutoff
                && Math.Abs(amount.X - complete.X) <= 8);
            var most = items.Max(amount => amount.Digits);
            return fragment ? complete with { Text = "", Digits = most } : complete;
        }

        var rejected = items.OrderByDescending(amount => amount.Digits).First();
        return rejected with { Text = "", Digits = rejected.Digits };
    }

    private static int DigitCount(string? text) => text is null ? 0 : text.Count(char.IsDigit);

    private readonly record struct Mark(int Width, int Height, int Count, int Center);

    private static Mark Measure(bool[] ink, int width, int left, int right, int top, int bottom)
    {
        var first = bottom;
        var last = top;
        var count = 0;
        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                if (!ink[(y * width) + x])
                {
                    continue;
                }

                count++;
                if (y < first)
                {
                    first = y;
                }

                if (y > last)
                {
                    last = y;
                }
            }
        }

        return new Mark(right - left, last - first + 1, count, (first + last) / 2);
    }

    private static bool Column(bool[] ink, int width, int x, int top, int bottom)
    {
        for (var y = top; y < bottom; y++)
        {
            if (ink[(y * width) + x])
            {
                return true;
            }
        }

        return false;
    }

    private static bool[,] Crop(bool[] ink, int width, int left, int right, int top, int bottom)
    {
        var grid = new bool[bottom - top, right - left];
        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                grid[y - top, x - left] = ink[(y * width) + x];
            }
        }

        return grid;
    }

    private readonly record struct Ranked(string Digit, string Other, double Best, double Second);

    /// <summary>
    /// A confident template wins. A close call between 0 and 8 follows the stroke: a diagonal slash is 0,
    /// and a bar with a hole on each side is 8. A close 3 or 9 is accepted only when its outline agrees.
    /// </summary>
    private static string? Classify(bool[,] crop, bool[,] body)
    {
        var matched = Match(crop);
        if (matched is "5" && Bowl(body))
        {
            return "6";
        }

        if (matched is "8" && Slashed(body) && !Eight(body))
        {
            return "0";
        }

        // A 3 is open down its left side. A 9 keeps the upper left closed.
        if (matched is "9" && OpenMiddle(body) && !Tail(body))
        {
            return "3";
        }

        if (matched is not null)
        {
            return matched;
        }

        var rank = Rank(crop);
        if (rank.Best >= 0.75)
        {
            // A zero the template prefers stays 0, including one whose slash splits the hole.
            // An 8 is used only when the other candidate is 8 and the holes sit on a bar, not on a slash.
            // A close 0 and 8. The slash stays 0. A bar with a hole on each side is an 8.
            if (rank.Digit is "0" && rank.Other is "8")
            {
                return Eight(body) && !Slashed(body) ? "8" : "0";
            }

            // Another digit is closer than 8. Keep the zero unless it really has a hole above and below a bar.
            if (rank.Digit is "0" && (Slashed(body) || !Eight(body)))
            {
                return "0";
            }

            if ((rank.Digit is "6" or "8") && (rank.Other is "6" or "8"))
            {
                if (Eight(body) && !Slashed(body))
                {
                    return "8";
                }

                if (Six(body) || (Bowl(body) && OpensRight(body)))
                {
                    return "6";
                }
            }

            if (rank.Digit is "6" && rank.Other is "0" && Bowl(body) && OpensRight(body))
            {
                return "6";
            }

            if ((rank.Digit is "5" or "9") && (rank.Other is "5" or "9") && OpensRight(body))
            {
                return "5";
            }

            if (rank.Digit is "5" && rank.Other is "6")
            {
                if (Bowl(body))
                {
                    return "6";
                }

                if (OpensRight(body))
                {
                    return "5";
                }
            }

            // A 3 is open on the left. The 9 and 5 templates still win the score when the stroke is thin.
            if (OpenMiddle(body) && (rank.Digit is "3" or "5" or "9") && (rank.Other is "3" or "5" or "9"))
            {
                return "3";
            }

            if (rank.Digit is "9" && (rank.Other is "8" or "0") && Tail(body))
            {
                return "9";
            }

            // A closed outline whose runner-up is 0 is a zero. Two holes from the slash are not an 8.
            if (rank.Digit is "9" && rank.Other is "0" && !Tail(body))
            {
                return "0";
            }
        }

        if (Eight(body) && !Slashed(body))
        {
            return "8";
        }

        if (Six(body))
        {
            return "6";
        }

        return Slashed(body) ? "0" : null;
    }

    private static string? Match(bool[,] glyph)
    {
        var rank = Rank(glyph);
        return rank.Best >= 0.75 && rank.Best - rank.Second >= 0.08 ? rank.Digit : null;
    }

    private static Ranked Rank(bool[,] glyph)
    {
        var fitted = Fit(glyph);
        var best = "";
        var other = "";
        var bestScore = 0.0;
        var second = 0.0;
        foreach (var (digit, template) in Templates)
        {
            var score = Score(fitted, template);
            if (score > bestScore)
            {
                if (!string.Equals(digit, best, StringComparison.Ordinal))
                {
                    second = bestScore;
                    other = best;
                }

                bestScore = score;
                best = digit;
            }
            else if (!string.Equals(digit, best, StringComparison.Ordinal) && score > second)
            {
                second = score;
                other = digit;
            }
        }

        return new Ranked(best, other, bestScore, second);
    }

    /// <summary>
    /// The gap inside a slashed zero moves from one side to the other. An 8's holes stay in the same column.
    /// </summary>
    private static bool Slashed(bool[,] glyph)
    {
        var height = glyph.GetLength(0);
        var width = glyph.GetLength(1);
        if (height < 6 || width < 4)
        {
            return false;
        }

        var upper = new List<double>();
        var lower = new List<double>();
        for (var y = 0; y < height; y++)
        {
            if (GapCenter(glyph, y) is not double center)
            {
                continue;
            }

            if (y < height / 2)
            {
                upper.Add(center);
            }
            else
            {
                lower.Add(center);
            }
        }

        if (upper.Count == 0 || lower.Count == 0)
        {
            return false;
        }

        var upperCenter = upper.Average();
        var shift = lower.Max(center => Math.Abs(center - upperCenter));
        return shift >= Math.Max(1.5, width * 0.25);
    }

    private static double? GapCenter(bool[,] glyph, int y)
    {
        var width = glyph.GetLength(1);
        var first = -1;
        var last = -1;
        for (var x = 0; x < width; x++)
        {
            if (!glyph[y, x])
            {
                continue;
            }

            if (first < 0)
            {
                first = x;
            }

            last = x;
        }

        if (first < 0 || last - first < 2)
        {
            return null;
        }

        var run = 0;
        var gapStart = 0;
        var bestStart = 0;
        var best = 0;
        for (var x = first + 1; x < last; x++)
        {
            if (glyph[y, x])
            {
                run = 0;
                continue;
            }

            if (run == 0)
            {
                gapStart = x;
            }

            run++;
            if (run > best)
            {
                best = run;
                bestStart = gapStart;
            }
        }

        return best == 0 ? null : bestStart + ((best - 1) / 2.0);
    }

    /// <summary>A 5 or a 6 leaves the upper right open. A 0 and a 9 keep that side closed.</summary>
    private static bool OpensRight(bool[,] glyph)
    {
        var height = glyph.GetLength(0);
        var width = glyph.GetLength(1);
        for (var y = 1; y < height / 2; y++)
        {
            var left = false;
            var right = false;
            for (var x = 0; x < width / 2; x++)
            {
                if (glyph[y, x])
                {
                    left = true;
                }
            }

            for (var x = width * 2 / 3; x < width; x++)
            {
                if (glyph[y, x])
                {
                    right = true;
                }
            }

            if (left && !right)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A 3 is open on the left between its top and bottom curves.</summary>
    private static bool OpenMiddle(bool[,] glyph)
    {
        var height = glyph.GetLength(0);
        var width = glyph.GetLength(1);
        var open = 0;
        var rows = 0;
        var limit = Math.Max(1, width * 2 / 5);
        for (var y = height / 3; y <= height * 2 / 3 && y < height; y++)
        {
            rows++;
            var ink = false;
            for (var x = 0; x < limit; x++)
            {
                if (glyph[y, x])
                {
                    ink = true;
                }
            }

            if (!ink)
            {
                open++;
            }
        }

        return rows > 0 && open * 2 >= rows;
    }

    /// <summary>A 9 has one hole in its upper half and a tail that leaves the lower left open.</summary>
    private static bool Tail(bool[,] glyph)
    {
        var height = glyph.GetLength(0);
        var width = glyph.GetLength(1);
        var holes = Holes(glyph);
        if (holes.Count != 1 || (holes[0].Top + holes[0].Bottom) / 2.0 > height * 0.55)
        {
            return false;
        }

        var open = 0;
        for (var y = height * 2 / 3; y < height; y++)
        {
            var left = false;
            var right = false;
            for (var x = 0; x < width / 2; x++)
            {
                if (glyph[y, x])
                {
                    left = true;
                }
            }

            for (var x = width / 2; x < width; x++)
            {
                if (glyph[y, x])
                {
                    right = true;
                }
            }

            if (!left && right)
            {
                open++;
            }
        }

        return open > 0;
    }

    private static bool[,] Tight(bool[,] glyph)
    {
        var height = glyph.GetLength(0);
        var width = glyph.GetLength(1);
        var top = height;
        var bottom = 0;
        var left = width;
        var right = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (!glyph[y, x])
                {
                    continue;
                }

                top = Math.Min(top, y);
                bottom = Math.Max(bottom, y);
                left = Math.Min(left, x);
                right = Math.Max(right, x);
            }
        }

        if (bottom < top || right < left)
        {
            return glyph;
        }

        var tight = new bool[bottom - top + 1, right - left + 1];
        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                tight[y - top, x - left] = glyph[y, x];
            }
        }

        return tight;
    }

    private static bool Bowl(bool[,] glyph)
    {
        var height = glyph.GetLength(0);
        var holes = Holes(glyph).Where(hole => hole.Bottom - hole.Top >= 1).ToList();
        if (holes.Count != 1)
        {
            return false;
        }

        var hole = holes[0];
        var center = (hole.Top + hole.Bottom) / 2.0;
        return hole.Top >= height * 0.35 && center > height * 0.55;
    }

    private static bool Six(bool[,] glyph)
    {
        if (!Bowl(glyph))
        {
            return false;
        }

        // A 6 is open on the upper right. The bottom of a 0 still has ink on both sides.
        var height = glyph.GetLength(0);
        var width = glyph.GetLength(1);
        var open = 0;
        var examined = Math.Max(1, height / 3);
        for (var y = 0; y < examined; y++)
        {
            var rightInk = false;
            for (var x = width * 2 / 3; x < width; x++)
            {
                if (glyph[y, x])
                {
                    rightInk = true;
                }
            }

            if (!rightInk)
            {
                open++;
            }
        }

        return open * 2 >= examined;
    }

    /// <summary>
    /// An 8 encloses one hole above a crossbar and another below it.
    /// A diagonal stroke across a zero can split its hole in two, so that stroke is not an 8.
    /// </summary>
    private static bool Eight(bool[,] glyph)
    {
        var height = glyph.GetLength(0);
        var holes = Holes(glyph);
        foreach (var above in holes)
        {
            if ((above.Top + above.Bottom) / 2.0 >= height * 0.5)
            {
                continue;
            }

            foreach (var below in holes)
            {
                if ((below.Top + below.Bottom) / 2.0 <= height * 0.5 || below.Top <= above.Bottom)
                {
                    continue;
                }

                var overlap = Math.Min(above.Right, below.Right) - Math.Max(above.Left, below.Left);
                if (overlap >= 1)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static List<(int Top, int Bottom, int Left, int Right)> Holes(bool[,] glyph)
    {
        var height = glyph.GetLength(0);
        var width = glyph.GetLength(1);
        var holes = new List<(int Top, int Bottom, int Left, int Right)>();
        if (height < 6 || width < 4)
        {
            return holes;
        }

        var outside = new bool[height, width];
        var stack = new Stack<(int Y, int X)>();
        for (var y = 0; y < height; y++)
        {
            TryPush(glyph, outside, stack, y, 0);
            TryPush(glyph, outside, stack, y, width - 1);
        }

        for (var x = 0; x < width; x++)
        {
            TryPush(glyph, outside, stack, 0, x);
            TryPush(glyph, outside, stack, height - 1, x);
        }

        while (stack.Count > 0)
        {
            var (y, x) = stack.Pop();
            TryPush(glyph, outside, stack, y - 1, x);
            TryPush(glyph, outside, stack, y + 1, x);
            TryPush(glyph, outside, stack, y, x - 1);
            TryPush(glyph, outside, stack, y, x + 1);
        }

        var seen = new bool[height, width];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (glyph[y, x] || outside[y, x] || seen[y, x])
                {
                    continue;
                }

                var area = 0;
                var top = y;
                var bottom = y;
                var left = x;
                var right = x;
                var fill = new Stack<(int Y, int X)>();
                fill.Push((y, x));
                seen[y, x] = true;
                while (fill.Count > 0)
                {
                    var (fy, fx) = fill.Pop();
                    area++;
                    top = Math.Min(top, fy);
                    bottom = Math.Max(bottom, fy);
                    left = Math.Min(left, fx);
                    right = Math.Max(right, fx);
                    void Take(int ny, int nx)
                    {
                        if (ny < 0 || nx < 0 || ny >= height || nx >= width || glyph[ny, nx] || outside[ny, nx] || seen[ny, nx])
                        {
                            return;
                        }

                        seen[ny, nx] = true;
                        fill.Push((ny, nx));
                    }

                    Take(fy - 1, fx);
                    Take(fy + 1, fx);
                    Take(fy, fx - 1);
                    Take(fy, fx + 1);
                }

                if (area < 4 || right - left < 1 || bottom - top < 1)
                {
                    continue;
                }

                holes.Add((top, bottom, left, right));
            }
        }

        return holes;
    }

    private static void TryPush(bool[,] glyph, bool[,] outside, Stack<(int Y, int X)> stack, int y, int x)
    {
        var height = glyph.GetLength(0);
        var width = glyph.GetLength(1);
        if (y < 0 || x < 0 || y >= height || x >= width || glyph[y, x] || outside[y, x])
        {
            return;
        }

        outside[y, x] = true;
        stack.Push((y, x));
    }

    private static double Score(bool[,] left, bool[,] right)
    {
        var same = 0;
        for (var y = 0; y < GridH; y++)
        {
            for (var x = 0; x < GridW; x++)
            {
                if (left[y, x] == right[y, x])
                {
                    same++;
                }
            }
        }

        return same / (double)(GridW * GridH);
    }

    private static bool[,] Fit(bool[,] source)
    {
        var top = 0;
        var bottom = source.GetLength(0) - 1;
        var left = 0;
        var right = source.GetLength(1) - 1;
        var grid = new bool[GridH, GridW];
        var height = bottom - top + 1;
        var width = right - left + 1;
        for (var y = 0; y < GridH; y++)
        {
            var sy = top + Math.Min(height - 1, (int)((y + 0.5) * height / GridH));
            for (var x = 0; x < GridW; x++)
            {
                var sx = left + Math.Min(width - 1, (int)((x + 0.5) * width / GridW));
                grid[y, x] = source[sy, sx];
            }
        }

        return grid;
    }

    private static (string Digit, bool[,] Grid)[] Build()
    {
        (string Digit, string[] Rows)[] samples =
        [
            ("5", ["#####.", "##....", "#.....", "####..", "#####.", "....##", "....##", "#...##", "#####."]),
            ("5", ["#####", "#####", "#....", "#....", "#####", "...##", "....#", "....#", "#####", ".###."]),
            ("3", [".####.", "##.###", "....##", "...###", "..####", "...###", "....##", "##..##", "#####."]),
            ("3", [".###..", "#####.", "...###", "....##", "..###.", "..###.", "....##", "....##", "#####.", ".###.."]),
            ("8", ["..###..", ".#####.", "##...##", "##...##", ".######", ".######", "###..##", "##...##", ".######", "..###.."]),
            ("0", ["..###..", ".######", "###.###", "##.####", "#######", "#######", "####.##", "###..##", ".#####.", "...##.."]),
            ("0", [".###", "#####", "#..##", "#.###", "#.###", "###.#", "###.#", "##..#", "#####"]),
            ("2", [".####.", "##..##", "....##", "....##", "..###.", ".###..", "##....", "##....", "######"]),
            ("4", ["...##.", "..###.", "..###.", ".####.", "##..#.", "##.###", "######", "...###", "....#."]),
            ("1", ["####", "..##", "..##", "..##", "..##", "..##", "..##", "..##", "..##"]),
            ("1", ["###", "###", ".##", ".##", ".##", ".##", ".##", ".##", ".##", "..#"]),
            ("1", ["###", "..#", "..#", "..#", "..#", "..#", "..#", "..#", "..#"]),
            ("2", [".#####.", ".#...#.", ".....##", "....##.", "..####.", ".##....", "##.....", "######.", "######."]),
            ("2", [".####.", "######", "....##", "....##", "..###.", ".###..", "##....", "##....", "######", "######"]),
            ("6", [".####.", "######", "##....", "####..", "######", "##..##", "##...#", "##..##", "######", ".####."]),
            ("6", [".#####.", "##...#.", "##.....", "######.", "#######", "##...##", "##...##", "###..##", ".#####."]),
            ("6", [".###..", "######", "#.....", "##....", "######", "##...#", "##...#", "##...#", ".#####", "..##.."]),
            ("5", ["#####.", "#####.", "##....", "##....", "#####.", "...###", "....##", "#...##", "#####.", ".###.."]),
            ("7", [".#####", "######", "....##", "....##", "...##.", "...##.", "..##..", "..##..", ".##..."]),
            ("7", ["..####", "######", ".....#", "....##", "...##.", "...##.", "..##..", "..#...", ".##..."]),
            ("9", [".####.", "######", "#...##", "#...##", "##..##", "######", "....##", "....##", "######", "..###."]),
            ("9", ["#####.", "#...##", "#...##", "#...##", "######", ".#####", "....##", "##..##", "#####."]),
            ("9", ["..###.", "######", "#....#", "#....#", "##..##", ".#####", ".....#", "....##", ".#####", ".####."]),
            ("9", [".####.", "######", "##...#", "##..##", "##..##", "######", "....##", "....##", "######", ".####."]),
            ("0", [".#####", "######", "##.###", "#.####", "######", "######", "###.##", "######", "#####."]),
            ("5", [".####.", "######", "##....", "##....", "#####.", "....##", "....##", "....##", "######", ".####."]),
            ("0", ["..###..", ".#####.", "##...#.", "##...##", "######.", "######.", "##...##", "##...##", "######.", ".####.."]),
            ("0", ["..###..", ".#####.", "##..###", "##.####", "##.####", "#######", "####.##", "###..##", ".#####.", "..####."]),
            ("8", [".#####.", "###.###", "##...##", "###.###", ".######", "###.###", "##...##", "###.###", ".#####."]),
        ];
        return samples.Select(sample => (sample.Digit, Fit(ToGrid(sample.Rows)))).ToArray();
    }

    private static bool[,] ToGrid(string[] rows)
    {
        var width = rows.Max(row => row.Length);
        var grid = new bool[rows.Length, width];
        for (var y = 0; y < rows.Length; y++)
        {
            for (var x = 0; x < rows[y].Length; x++)
            {
                grid[y, x] = rows[y][x] == '#';
            }
        }

        return grid;
    }
}
