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
}
