using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.Core.Tests;

public class HudNumberReaderTests
{
    [Fact]
    public void The_captured_signature_shapes_read_as_10800_and_2000()
    {
        var ten = Row(
            Glyph(
                ".###.",
                "####.",
                "####.",
                ".###.",
                ".###.",
                ".###.",
                ".###.",
                ".###.",
                ".####",
                "#####",
                "####."),
            Glyph(
                "..####.",
                ".######",
                "###.###",
                "###.###",
                "###.###",
                "###.###",
                "###.###",
                "###.###",
                "######.",
                ".#####.",
                "...#..."),
            Comma(),
            Glyph(
                "..#####",
                ".######",
                ".##.###",
                "###.###",
                ".######",
                ".######",
                ".##.###",
                "###..##",
                "#######",
                ".#####.",
                "..###.."),
            Glyph(
                "...###.",
                ".######",
                ".##.###",
                ".##..##",
                "###..##",
                "###..##",
                ".##..##",
                ".##..##",
                ".######",
                ".#####.",
                "..###.."),
            Glyph(
                "..###.",
                ".#####",
                "###.##",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                "###.##",
                "######",
                "#####.",
                "..#..."));

        var two = Row(
            Glyph(
                "...#...",
                ".######",
                ".######",
                "##..###",
                "....###",
                "..####.",
                ".####..",
                "###....",
                "###....",
                "#######",
                "#######"),
            Comma(),
            Glyph(
                "....#..",
                "..#####",
                ".######",
                ".##..##",
                "###..##",
                "###..##",
                "###..##",
                "###..##",
                "###.###",
                ".#####.",
                "..###.."),
            Glyph(
                "....#..",
                "..#####",
                ".######",
                ".##..##",
                "###..##",
                "###..##",
                "###..##",
                "###..##",
                ".######",
                ".#####.",
                "..###.."),
            Glyph(
                "..#####",
                ".######",
                ".##..##",
                ".##..##",
                "###..##",
                "###..##",
                "###..##",
                ".###.##",
                ".#####.",
                "..###.."));

        Assert.Equal("10800", Read(ten));
        Assert.Equal("2000", Read(two));
        Assert.Equal("7200", Read(Row(
            Glyph(
                ".######",
                ".######",
                "#######",
                "....###",
                "....##.",
                "...###.",
                "...###.",
                "...###.",
                "..###..",
                "..###..",
                "..##...",
                "..##..."),
            Comma(),
            Glyph(
                "..###.",
                ".#####",
                "######",
                "##.###",
                "#..###",
                "...##.",
                "..###.",
                "..##..",
                ".##...",
                "#####.",
                "######",
                "#####."),
            Glyph(
                "...###.",
                ".######",
                ".######",
                "###.###",
                "###.###",
                "###.###",
                "###.###",
                "###.###",
                "###.###",
                "###.###",
                "######.",
                ".####.."),
            Glyph(
                "...##..",
                ".######",
                ".######",
                "###.###",
                "###.###",
                "###.###",
                "###.###",
                "###.###",
                "###.###",
                "###.###",
                "######.",
                ".####.."))));
    }

    [Fact]
    public void A_wide_marker_beside_a_digit_is_not_a_number()
    {
        var rows = Row(
            Glyph(
                "################",
                "################",
                "################",
                "################",
                "################",
                "################",
                "################",
                "################",
                "################",
                "################",
                "################",
                "################",
                "################",
                "################"),
            Glyph(
                ".###.",
                "####.",
                "####.",
                ".###.",
                ".###.",
                ".###.",
                ".###.",
                ".###.",
                ".####",
                "#####",
                "####."));

        Assert.Equal("1", Read(rows));
    }

    [Fact]
    public void The_captured_8250_keeps_the_five()
    {
        Assert.Equal("8250", Read(Row(
            Glyph(
                "..###..",
                ".#####.",
                "###.###",
                "##...##",
                "##.###.",
                ".####..",
                "###.##.",
                "##..###",
                "##..##.",
                "######.",
                "..##..."),
            Comma(),
            Glyph(
                "...###.",
                ".######",
                ".##.###",
                "##...##",
                "....###",
                "..####.",
                ".####..",
                "###....",
                "###....",
                "######.",
                ".#####."),
            Glyph(
                "######",
                "##....",
                "##....",
                "#####.",
                "######",
                "....##",
                "....##",
                "##..##",
                "##.##.",
                ".####."),
            Glyph(
                ".###..",
                "######",
                "##...#",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                "##.###",
                ".###.."))));
    }

    [Fact]
    public void A_rounded_signature_five_still_reads_as_8250()
    {
        Assert.Equal("8250", Read(Row(
            Glyph(
                "..##..",
                ".####.",
                "#...##",
                "#...#.",
                ".#.##.",
                ".###..",
                "#...#.",
                "#...##",
                "##..#.",
                "..##.."),
            Comma(),
            Glyph(
                "..##.",
                ".####",
                "##.##",
                "#..##",
                "...##",
                ".###.",
                "##...",
                "##...",
                "##...",
                "#####"),
            Glyph(
                ".####.",
                "##....",
                "##....",
                "#####.",
                "....##",
                "....##",
                ".#..##",
                ".#..#.",
                ".####."),
            Glyph(
                "..##..",
                ".##.##",
                ".#...#",
                ".#..##",
                "##..##",
                ".#..##",
                ".#..##",
                ".#..##",
                "..###."))));
    }

    [Fact]
    public void The_remaining_digit_shapes_read_as_themselves()
    {
        Assert.Equal("3", Read(Row(Pad(), Glyph(
            ".######",
            ".....##",
            ".....##",
            "..#####",
            ".######",
            ".....##",
            "....###",
            "##...##",
            ".######",
            "..####."))));
        Assert.Equal("4", Read(Row(Pad(), Glyph(
            "###..##",
            "##...##",
            "##...##",
            "#######",
            ".######",
            ".....##",
            ".....##",
            ".....##",
            ".....##",
            "....##."))));
        Assert.Equal("6", Read(Row(Pad(), Glyph(
            "..####",
            ".##...",
            "##....",
            "##....",
            ".#####",
            "######",
            "##..##",
            "##..##",
            "##..##",
            ".#####"))));
        Assert.Equal("9", Read(Row(Pad(), Glyph(
            ".####.",
            "##..##",
            "##..##",
            "##..##",
            "######",
            ".#####",
            "....##",
            "....##",
            "...##.",
            "..####"))));
    }

    [Fact]
    public void A_snowy_five_and_nine_still_read_as_59000()
    {
        Assert.Equal("59000", Read(Row(
            Glyph(
                "####.",
                "#####",
                "#....",
                "#....",
                "#....",
                "####.",
                "##.##",
                "#..##",
                "...##",
                "#..##",
                "#####",
                "####."),
            Glyph(
                "..##..",
                ".#####",
                ".##.##",
                "##..##",
                "##..##",
                "##..##",
                ".##.##",
                ".#####",
                ".#...#",
                ".##.##",
                ".#####",
                ".####."),
            Glyph(
                "..#...",
                ".####.",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                ".####."),
            Glyph(
                "..#...",
                ".####.",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                ".####."),
            Glyph(
                "..#...",
                ".####.",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                "##..##",
                ".####."))));
    }

    [Fact]
    public void A_loose_rectangle_finds_the_digit_band_and_a_cut_edge_is_clipped()
    {
        var digit = Row(Pad(), Glyph(
            "...#...",
            ".######",
            ".######",
            "##..###",
            "....###",
            "..####.",
            ".####..",
            "###....",
            "###....",
            "#######",
            "#######"));
        var cut = Inspect(digit);
        Assert.Equal("2", Assert.Single(cut.Lines));
        Assert.True(cut.Clipped);

        var loose = Inspect(Frame(digit, 4));
        Assert.Equal("2", Assert.Single(loose.Lines));
        Assert.False(loose.Clipped);

        var bar = new string('#', digit[0].Length);
        var blank = new string('.', digit[0].Length);
        var crowded = Inspect([bar, bar, bar, blank, blank, blank, .. Frame(digit, 4)]);
        Assert.Equal("2", Assert.Single(crowded.Lines));
        Assert.False(crowded.Clipped);
    }

    private static string Read(string[] rows)
    {
        var width = rows.Max(row => row.Length);
        var data = new byte[width * rows.Length];
        for (var y = 0; y < rows.Length; y++)
        {
            for (var x = 0; x < rows[y].Length; x++)
            {
                data[y * width + x] = rows[y][x] == '#' ? (byte)255 : (byte)0;
            }
        }

        return Assert.Single(HudNumberReader.Read(data, width, rows.Length));
    }

    private static HudInspection Inspect(string[] rows)
    {
        var width = rows.Max(row => row.Length);
        var data = new byte[width * rows.Length];
        for (var y = 0; y < rows.Length; y++)
        {
            for (var x = 0; x < rows[y].Length; x++)
            {
                data[y * width + x] = rows[y][x] == '#' ? (byte)255 : (byte)0;
            }
        }

        return HudNumberReader.Inspect(data, width, rows.Length);
    }

    private static string[] Frame(string[] rows, int pad)
    {
        var inner = rows.Max(row => row.Length);
        var width = inner + (pad * 2);
        var blank = new string('.', width);
        var framed = new List<string>();
        for (var i = 0; i < pad; i++)
        {
            framed.Add(blank);
        }

        foreach (var row in rows)
        {
            framed.Add(new string('.', pad) + row.PadRight(inner, '.') + new string('.', pad));
        }

        for (var i = 0; i < pad; i++)
        {
            framed.Add(blank);
        }

        return framed.ToArray();
    }

    private static string[] Glyph(params string[] rows) => rows;

    private static string[] Pad() => ["........", "........", "........", "........", "........", "........", "........", "........", "........", "........"];

    private static string[] Comma() => ["...", "...", "...", "...", "...", "...", "...", "...", ".##", ".##", "##."];

    private static string[] Row(params string[][] glyphs)
    {
        var height = glyphs.Max(glyph => glyph.Length);
        var width = glyphs.Sum(glyph => glyph.Max(row => row.Length)) + (glyphs.Length - 1) * 2;
        var canvas = Enumerable.Range(0, height).Select(_ => new char[width]).ToArray();
        foreach (var line in canvas)
        {
            Array.Fill(line, '.');
        }

        var x = 0;
        foreach (var glyph in glyphs)
        {
            var glyphWidth = glyph.Max(row => row.Length);
            var top = height - glyph.Length;
            for (var y = 0; y < glyph.Length; y++)
            {
                for (var column = 0; column < glyph[y].Length; column++)
                {
                    canvas[top + y][x + column] = glyph[y][column];
                }
            }

            x += glyphWidth + 2;
        }

        return canvas.Select(line => new string(line)).ToArray();
    }
}
