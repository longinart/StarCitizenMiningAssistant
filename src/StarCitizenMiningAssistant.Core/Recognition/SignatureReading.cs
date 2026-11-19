using System.Globalization;
using StarCitizenMiningAssistant.Core.Equipment;

namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// Promotes one signature reading when it matches exactly one catalog deposit.
/// Decision 013. A miss, a blank, or two matches stays unconfirmed.
/// </summary>
public static class SignatureReading
{
    public static ObservedValue<double> FromLines(
        MiningCatalog catalog,
        IReadOnlyList<ObservedValue<string>> lines,
        ScanSignatureChart? chart = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(lines);
        var numbers = new List<double>();
        foreach (var line in lines)
        {
            if (line.Value is string text
                && NumberText.TryParseFirst(text, CultureInfo.InvariantCulture, out var number)
                && text.Any(char.IsDigit))
            {
                numbers.Add(number);
            }
        }

        if (numbers.Count == 0)
        {
            return ObservedValue<double>.Missing();
        }

        var reading = numbers[0];
        if (numbers.Exists(number => Math.Abs(number - reading) >= 0.001))
        {
            return ObservedValue<double>.Uncertain(reading);
        }

        var rounded = (int)Math.Round(reading);
        var shipCount = chart?.Matches(rounded).Count(match => !SignatureGroups.IsChartGroup(match.Kind)) ?? 0;
        var grouped = SignatureGroups.Multiple(rounded, SignatureGroups.SalvageBase) is not null
            || SignatureGroups.Multiple(rounded, SignatureGroups.HandAndRocBase) is not null;
        var confirmed = shipCount == 1 || (shipCount == 0 && (grouped || catalog.MatchSignature(reading).Count == 1));
        return confirmed
            ? ObservedValue<double>.Confirmed(reading)
            : ObservedValue<double>.Uncertain(reading);
    }
}
