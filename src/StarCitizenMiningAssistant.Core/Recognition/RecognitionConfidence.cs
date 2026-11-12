namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// How strongly a recognised value may be trusted.
/// Uncertain and missing values must never be treated as confirmed facts.
/// </summary>
public enum RecognitionConfidence
{
    Missing = 0,
    Uncertain = 1,
    Confirmed = 2
}
