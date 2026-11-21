namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// The mining-laser mode printed on the HUD. The phrase stays uncertain:
/// a read is only the gate for the composition patch, not a rock measurement.
/// </summary>
public static class MiningMode
{
    public static ObservedValue<string> Read(IEnumerable<string> words)
    {
        ArgumentNullException.ThrowIfNull(words);
        var text = string.Join(
            ' ',
            string.Join(' ', words).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (text.Contains("FRACTURE MODE", StringComparison.OrdinalIgnoreCase))
        {
            return ObservedValue<string>.Uncertain("FRACTURE MODE");
        }

        if (text.Contains("EXTRACTION MODE", StringComparison.OrdinalIgnoreCase))
        {
            return ObservedValue<string>.Uncertain("EXTRACTION MODE");
        }

        return ObservedValue<string>.Missing();
    }

    public static bool AllowsComposition(ObservedValue<string> mode) =>
        mode.Confidence != RecognitionConfidence.Missing
        && mode.Value is "FRACTURE MODE" or "EXTRACTION MODE";
}
