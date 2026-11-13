namespace StarCitizenMiningAssistant.Core.Recognition;

public interface IScreenReader
{
    Task<OcrDocument> ReadAsync(byte[] image, CancellationToken cancellationToken = default);
}
