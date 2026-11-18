using System.Runtime.InteropServices.WindowsRuntime;
using StarCitizenMiningAssistant.Core.Recognition;
using StarCitizenMiningAssistant.Infrastructure.Capture;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace StarCitizenMiningAssistant.Core.Tests;

public class HudSignatureReaderTests
{
    public static IEnumerable<object[]> Readouts() =>
        Directory.GetFiles(FixtureFolder(), "*.png")
            .Where(path => !Path.GetFileNameWithoutExtension(path).EndsWith("_none", StringComparison.Ordinal))
            .Order()
            .Select(path => new object[] { path });

    public static IEnumerable<object[]> OtherHud() =>
        Directory.GetFiles(FixtureFolder(), "*_none.png").Order().Select(path => new object[] { path });

    [Theory]
    [MemberData(nameof(Readouts))]
    public async Task A_readout_crop_matches_the_visible_number(string path)
    {
        var digits = Path.GetFileNameWithoutExtension(path).Split('_')[1];
        var expected = digits[..^3] + "," + digits[^3..];
        var image = await Channels(path);
        var hit = HudSignatureReader.Read(image.Width, image.Height, image.Red, image.Green, image.Blue, ScanPatch.DigitHeightAt1600, Chart());
        Assert.NotNull(hit);
        Assert.Equal(expected, hit.Value.Text);
    }

    [Theory]
    [MemberData(nameof(OtherHud))]
    public async Task Other_hud_text_is_not_a_signature(string path)
    {
        var image = await Channels(path);
        var hit = HudSignatureReader.Read(image.Width, image.Height, image.Red, image.Green, image.Blue, ScanPatch.DigitHeightAt1600, Chart());
        Assert.Null(hit);
    }

    [Theory]
    [InlineData("live-3400.png", "3,400")]
    [InlineData("live-6800.png", "6,800")]
    [InlineData("live-3170.png", "3,170")]
    [InlineData("live-16960.png", "16,960")]
    [InlineData("live-63000.png", "63,000")]
    [InlineData("live-45000.png", "45,000")]
    [InlineData("live-63000-broken.png", "63,000")]
    [InlineData("live-63000-tilt.png", "63,000")]
    [InlineData("live-logo.png", "")]
    [InlineData("live-interior.png", "")]
    [InlineData("live-bright.png", "")]
    public async Task A_game_patch_reads_the_number_that_was_on_screen(string name, string expected)
    {
        var image = await Channels(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Live", name));
        var hit = HudSignatureReader.Read(
            image.Width,
            image.Height,
            image.Red,
            image.Green,
            image.Blue,
            ScanPatch.DigitHeight(1440),
            Chart());
        Assert.Equal(expected, hit?.Text ?? "");
    }


    [Fact]
    public async Task The_scan_result_column_reads_mass_resistance_and_instability()
    {
        var image = await Channels(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Live", "rock-panel-5380.png"));
        var amounts = RockDigitReader.Read(image.Width, image.Height, image.Green);
        var mass = amounts.First(amount => amount.Text == "5380");
        var instability = amounts.First(amount => amount.Text == "5.32");
        var words = new[]
        {
            new RockPanel.Word("MASS:", 10, mass.Y, 12),
            new RockPanel.Word("RESISTANCE:", 10, mass.Y + 24, 12),
            new RockPanel.Word("0%", 80, mass.Y + 24, 12),
            new RockPanel.Word("INSTABILITY:", 10, instability.Y, 12),
            new RockPanel.Word("61.28%", 10, 160, 12)
        };

        var rock = RockPanel.Read(words, amounts);

        Assert.Equal(5380, rock.Mass.Value);
        Assert.Equal(0, rock.Resistance.Value);
        Assert.Equal(5.32, rock.Instability.Value);
        Assert.True(rock.HasPanel);
    }

    [Theory]
    [InlineData("rock-panel-5380-live.png", 5380, 0, 5.32)]
    [InlineData("rock-panel-12218.png", 12218, 47, 159)]
    [InlineData("rock-panel-51296.png", 51296, 0, 51.69)]
    public async Task A_logged_panel_keeps_the_decimal_and_the_percent(
        string name,
        double mass,
        double resistance,
        double instability)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Live", name);
        var image = await Channels(path);
        var words = await new WindowsOcrReader().ReadWordsAsync(await File.ReadAllBytesAsync(path));
        var rock = RockPanel.Read(words, RockDigitReader.Read(image.Width, image.Height, image.Green));

        Assert.Equal(mass, rock.Mass.Value);
        Assert.Equal(resistance, rock.Resistance.Value);
        Assert.Equal(instability, rock.Instability.Value);
    }

    [Fact]
    public async Task An_eight_is_not_read_as_a_zero_and_a_six_keeps_its_bowl()
    {
        var eight = await Channels(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Live", "rock-panel-6829.png"));
        var amounts = RockDigitReader.Read(eight.Width, eight.Height, eight.Green);
        Assert.Contains(amounts, amount => amount.Text == "6829");
        Assert.DoesNotContain(amounts, amount => amount.Text is "6029" or "6");

        var six = await Channels(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Live", "rock-panel-611.png"));
        var instability = RockDigitReader.Read(six.Width, six.Height, six.Green);
        Assert.Contains(instability, amount => amount.Text == "6.11");
        Assert.DoesNotContain(instability, amount => amount.Text == "5.11");
    }

    [Theory]
    [InlineData("rock-panel-10895.png", "10895", "1")]
    [InlineData("rock-panel-3402.png", "3402", "302")]
    [InlineData("rock-panel-5613.png", "5613", "5619")]
    [InlineData("rock-panel-1595.png", "1595", "1")]
    [InlineData("rock-panel-1038.png", "1038", "1")]
    [InlineData("rock-panel-22625.png", "22625", "2")]
    [InlineData("rock-panel-705.png", "705", "7")]
    public async Task A_slashed_zero_stays_zero_and_a_short_fragment_is_not_kept(string name, string mass, string fragment)
    {
        var image = await Channels(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Live", name));
        var amounts = RockDigitReader.Read(image.Width, image.Height, image.Green);

        Assert.Contains(amounts, amount => amount.Text == mass);
        Assert.DoesNotContain(amounts, amount => amount.Text == fragment);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(-18)]
    public async Task A_tilted_patch_is_read_after_it_is_turned_flat(int degrees)
    {
        var image = await Channels(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Live", "live-6800.png"));
        // The bright mark on the far left is not the number. The tilt under test is the digit row.
        for (var i = 0; i < image.Green.Length; i++)
        {
            if (i % image.Width < 250)
            {
                image.Red[i] = 0;
                image.Green[i] = 0;
                image.Blue[i] = 0;
            }
        }

        long sx = 0;
        long sy = 0;
        var count = 0;
        for (var i = 0; i < image.Green.Length; i++)
        {
            var x = i % image.Width;
            if (image.Green[i] < 180 || x < 250)
            {
                continue;
            }

            sx += x;
            sy += i / image.Width;
            count++;
        }

        var tilted = Tilt(image, degrees, (int)(sx / count), (int)(sy / count));
        var hit = HudSignatureReader.Read(
            tilted.Width,
            tilted.Height,
            tilted.Red,
            tilted.Green,
            tilted.Blue,
            ScanPatch.DigitHeight(1440),
            Chart());
        Assert.Equal("6,800", hit?.Text);
    }

    [Fact]
    public void Lindinium_times_two_is_one_deposit()
    {
        var matches = Chart().Matches(6800);
        Assert.Equal([new SignatureDeposit("lindinium", 2)], matches);
    }

    [Fact]
    public void A_new_signature_waits_for_a_second_identical_frame()
    {
        var gate = new RepeatedReading();
        Assert.Equal(RecognitionConfidence.Missing, gate.Gate(ObservedValue<double>.Confirmed(3400)).Confidence);
        var second = gate.Gate(ObservedValue<double>.Confirmed(3400));
        Assert.Equal(RecognitionConfidence.Confirmed, second.Confidence);
        Assert.Equal(3400, second.Value);
        Assert.Equal(RecognitionConfidence.Missing, gate.Gate(ObservedValue<double>.Confirmed(6800)).Confidence);
        Assert.Equal(RecognitionConfidence.Missing, gate.Gate(ObservedValue<double>.Missing()).Confidence);
    }


    private static async Task<(int Width, int Height, byte[] Red, byte[] Green, byte[] Blue)> Channels(string path)
    {
        await using var file = File.OpenRead(path);
        using var stream = file.AsRandomAccessStream();
        var decoder = await BitmapDecoder.CreateAsync(stream);
        using var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyToBuffer(pixels.AsBuffer());
        var red = new byte[bitmap.PixelWidth * bitmap.PixelHeight];
        var green = new byte[red.Length];
        var blue = new byte[red.Length];
        for (var i = 0; i < red.Length; i++)
        {
            blue[i] = pixels[i * 4];
            green[i] = pixels[(i * 4) + 1];
            red[i] = pixels[(i * 4) + 2];
        }

        return (bitmap.PixelWidth, bitmap.PixelHeight, red, green, blue);
    }

    private static (int Width, int Height, byte[] Red, byte[] Green, byte[] Blue) Tilt(
        (int Width, int Height, byte[] Red, byte[] Green, byte[] Blue) image,
        int degrees,
        int originX,
        int originY)
    {
        var radians = degrees * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var red = new byte[image.Red.Length];
        var green = new byte[image.Green.Length];
        var blue = new byte[image.Blue.Length];
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var dx = x - originX;
                var dy = y - originY;
                var sourceX = (int)Math.Round(originX + (cos * dx) + (sin * dy));
                var sourceY = (int)Math.Round(originY - (sin * dx) + (cos * dy));
                var index = (y * image.Width) + x;
                if (sourceX < 0 || sourceY < 0 || sourceX >= image.Width || sourceY >= image.Height)
                {
                    continue;
                }

                var source = (sourceY * image.Width) + sourceX;
                red[index] = image.Red[source];
                green[index] = image.Green[source];
                blue[index] = image.Blue[source];
            }
        }

        return (image.Width, image.Height, red, green, blue);
    }

    private static string FixtureFolder()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Sco");
        if (!Directory.Exists(folder))
        {
            throw new DirectoryNotFoundException(folder);
        }

        return folder;
    }

    private static ScanSignatureChart Chart()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "data", "scan-signatures.json");
            if (File.Exists(candidate))
            {
                return ScanSignatureChart.Parse(File.ReadAllText(candidate));
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("data/scan-signatures.json was not found above the test output.");
    }
}
