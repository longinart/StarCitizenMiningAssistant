using StarCitizenMiningAssistant.Core.Equipment;
using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.Infrastructure.Capture;

/// <summary>
/// Reads a signature rectangle by cleaning the digit row and asking Windows OCR
/// twice. A disagreement is an empty sample. Decision 018.
/// </summary>
public static class SignatureOcr
{
    public static async Task<OcrDocument> ReadAsync(byte[] image, MiningCatalog? catalog, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        var prepared = SignatureDigitStrip.Prepare(image);
        if (!prepared.Found)
        {
            return OcrDocument.FromEngine("signature digits", []);
        }

        var reader = new WindowsOcrReader();
        var smooth = await reader.ReadPreparedAsync(prepared.Smooth, cancellationToken);
        var sharp = await reader.ReadPreparedAsync(prepared.Sharp, cancellationToken);
        if (smooth.Failure is not null && sharp.Failure is not null)
        {
            return smooth;
        }

        var chosen = SignatureDigits.Choose(Text(smooth), Text(sharp), catalog);
        if (chosen is null)
        {
            return OcrDocument.FromEngine("signature digits", []);
        }

        return OcrDocument.FromEngine("signature digits", [new OcrFragment(chosen, null)]);
    }

    private static string? Text(OcrDocument document) =>
        document.Failure is null && document.Lines.Count > 0 ? document.Lines[0].Value : null;
}
