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
    public Task<OcrDocument> ReadAsync(byte[] image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        return RecognizeAsync(image, digitsFirst: true, cancellationToken);
    }

    public Task<OcrDocument> ReadLabelsAsync(byte[] image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        return RecognizeAsync(ScaleBmp(image), digitsFirst: false, cancellationToken);
    }

    private async Task<OcrDocument> RecognizeAsync(byte[] image, bool digitsFirst, CancellationToken cancellationToken)
    {
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
            if (digitsFirst && TryReadBmp(image, out var luminance, out var width, out var height))
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

    private static byte[] ScaleBmp(byte[] image)
    {
        if (!TryMetrics(image, out var pixelOffset, out var width, out var height, out var stride))
        {
            return image;
        }

        var factor = 3;
        while (factor > 1 && (width * (long)factor > 2000 || height * (long)factor > 2000))
        {
            factor--;
        }

        if (factor <= 1)
        {
            return image;
        }

        var scaledWidth = width * factor;
        var scaledHeight = height * factor;
        var scaledStride = ((scaledWidth * 3 + 3) / 4) * 4;
        var pixels = new byte[scaledStride * scaledHeight];
        for (var y = 0; y < scaledHeight; y++)
        {
            var sourceY = y / factor;
            for (var x = 0; x < scaledWidth; x++)
            {
                var source = pixelOffset + (sourceY * stride) + ((x / factor) * 3);
                var dest = (y * scaledStride) + (x * 3);
                pixels[dest] = image[source];
                pixels[dest + 1] = image[source + 1];
                pixels[dest + 2] = image[source + 2];
            }
        }

        var file = new byte[54 + pixels.Length];
        file[0] = (byte)'B';
        file[1] = (byte)'M';
        WriteInt(file, 2, file.Length);
        WriteInt(file, 10, 54);
        WriteInt(file, 14, 40);
        WriteInt(file, 18, scaledWidth);
        WriteInt(file, 22, scaledHeight);
        file[26] = 1;
        file[28] = 24;
        WriteInt(file, 34, pixels.Length);
        Buffer.BlockCopy(pixels, 0, file, 54, pixels.Length);
        return file;
    }

    private static bool TryMetrics(byte[] image, out int pixelOffset, out int width, out int height, out int stride)
    {
        pixelOffset = 0;
        width = 0;
        height = 0;
        stride = 0;
        if (image.Length < 54 || image[0] != (byte)'B' || image[1] != (byte)'M')
        {
            return false;
        }

        pixelOffset = BitConverter.ToInt32(image, 10);
        width = BitConverter.ToInt32(image, 18);
        height = BitConverter.ToInt32(image, 22);
        var bitCount = BitConverter.ToInt16(image, 28);
        stride = ((width * 3 + 3) / 4) * 4;
        return width >= 8 && height >= 8 && bitCount == 24 && pixelOffset >= 54
            && pixelOffset + (stride * height) <= image.Length;
    }

    private static void WriteInt(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
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
