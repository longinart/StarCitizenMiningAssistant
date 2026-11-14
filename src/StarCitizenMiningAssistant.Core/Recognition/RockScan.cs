using System.Globalization;

namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// Turns a saved rock field into the confidence it was stored with.
/// A blank confidence is a number the user typed, so it is confirmed.
/// A scan marked uncertain stays uncertain.
/// </summary>
public static class RockScan
{
    public static ObservedValue<double> Value(string? text, string? confidence)
    {
        if (string.Equals(confidence?.Trim(), "missing", StringComparison.OrdinalIgnoreCase))
        {
            return ObservedValue<double>.Missing();
        }

        if (!TryParse(text, out var number))
        {
            return ObservedValue<double>.Missing();
        }

        if (string.Equals(confidence?.Trim(), "uncertain", StringComparison.OrdinalIgnoreCase))
        {
            return ObservedValue<double>.Uncertain(number);
        }

        return ObservedValue<double>.Confirmed(number);
    }

    public static string ConfidenceName(RecognitionConfidence confidence) =>
        confidence switch
        {
            RecognitionConfidence.Missing => "missing",
            RecognitionConfidence.Uncertain => "uncertain",
            _ => "confirmed"
        };

    private static bool TryParse(string? text, out double value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0;
            return false;
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
            || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
