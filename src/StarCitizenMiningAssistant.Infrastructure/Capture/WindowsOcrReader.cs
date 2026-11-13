using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Versioning;
using StarCitizenMiningAssistant.Core.Recognition;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace StarCitizenMiningAssistant.Infrastructure.Capture;

[SupportedOSPlatform("windows10.0.19041.0")]
public sealed class WindowsOcrReader : IScreenReader
{
    public async Task<OcrDocument> ReadAsync(byte[] image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (image.Length < 54)
        {
            return OcrDocument.Failed("The image was empty.");
        }

        var english = new Language("en");
        var engine = OcrEngine.IsLanguageSupported(english)
            ? OcrEngine.TryCreateFromLanguage(english)
            : OcrEngine.TryCreateFromUserProfileLanguages();
        if (engine is null)
        {
            return OcrDocument.Failed("Windows OCR has no language installed.");
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (TryReadBmp(image, out var luminance, out var width, out var height))
            {
                var digits = HudNumberReader.Read(luminance, width, height);
                if (digits.Any(line => line.Any(char.IsDigit)))
                {
                    return OcrDocument.FromEngine(
                        "interface digits",
                        digits.Select(line => new OcrFragment(line, null)).ToList());
                }
            }

            using var memory = new MemoryStream(image);
            using var stream = memory.AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(stream);
            using var bitmap = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied);
            var result = await engine.RecognizeAsync(bitmap);
            var fragments = result.Lines
                .Select(line => new OcrFragment(line.Text, null))
                .ToList();
            return OcrDocument.FromEngine(engine.RecognizerLanguage.LanguageTag, fragments);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return OcrDocument.Failed("The text could not be read. " + ex.Message);
        }
    }

    private static bool TryReadBmp(byte[] image, out byte[] luminance, out int width, out int height)
    {
        luminance = [];
        width = 0;
        height = 0;
        if (image.Length < 54 || image[0] != (byte)'B' || image[1] != (byte)'M')
        {
            return false;
        }

        var pixelOffset = BitConverter.ToInt32(image, 10);
        width = BitConverter.ToInt32(image, 18);
        height = BitConverter.ToInt32(image, 22);
        var bitCount = BitConverter.ToInt16(image, 28);
        if (width < 8 || height < 8 || bitCount != 24 || pixelOffset < 54 || pixelOffset >= image.Length)
        {
            return false;
        }

        var stride = ((width * 3 + 3) / 4) * 4;
        if (pixelOffset + (stride * height) > image.Length)
        {
            return false;
        }

        luminance = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            var fileRow = height - 1 - y;
            for (var x = 0; x < width; x++)
            {
                var index = pixelOffset + (fileRow * stride) + (x * 3);
                luminance[(y * width) + x] = (byte)Math.Max(image[index], Math.Max(image[index + 1], image[index + 2]));
            }
        }

        return true;
    }
}
