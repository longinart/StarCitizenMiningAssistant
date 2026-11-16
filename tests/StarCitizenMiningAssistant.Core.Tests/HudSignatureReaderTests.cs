using System.Runtime.InteropServices.WindowsRuntime;
using StarCitizenMiningAssistant.Core.Recognition;
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
    [InlineData("live-63000-broken.png", "")]
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
