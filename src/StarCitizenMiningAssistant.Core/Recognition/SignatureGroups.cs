namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// Salvage and hand or ROC deposits are any positive multiple of a base.
/// The scanner chart caps those rows, so a count such as 27 stays here.
/// </summary>
public static class SignatureGroups
{
    public const int SalvageBase = 2000;

    public const int HandAndRocBase = 3000;

    public const string Salvage = "Salvage";

    public const string HandAndRoc = "Hand & ROC";

    public static bool IsChartGroup(string kind) =>
        kind.Equals("salvage", StringComparison.OrdinalIgnoreCase)
        || kind.Equals("fps", StringComparison.OrdinalIgnoreCase)
        || kind.Equals("roc", StringComparison.OrdinalIgnoreCase);

    public static int? Multiple(int value, int step) =>
        step > 0 && value >= step && value % step == 0 ? value / step : null;
}
