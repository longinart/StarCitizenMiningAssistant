using System.Globalization;
using StarCitizenMiningAssistant.Core.Refining;

namespace StarCitizenMiningAssistant.Core.Recognition;

public sealed class RockOcrFields
{
    public required ObservedValue<double> Mass { get; init; }

    public required ObservedValue<double> Resistance { get; init; }

    public required ObservedValue<double> Instability { get; init; }

    public IReadOnlyList<ObservedShare> Composition { get; init; } = [];

    public ObservedValue<double> TotalScu { get; init; } = ObservedValue<double>.Missing();

    /// <summary>
    /// The scan-results panel is present when any of the three numbers was read.
    /// A missing resistance does not hide a mass that was read. Crackability still
    /// needs both mass and resistance before it calls the rock crackable.
    /// </summary>
    public bool HasPanel =>
        Mass.Confidence != RecognitionConfidence.Missing
        || Resistance.Confidence != RecognitionConfidence.Missing
        || Instability.Confidence != RecognitionConfidence.Missing;

    public static RockOcrFields Read(IReadOnlyList<ObservedValue<string>> lines) =>
        Read(lines, CultureInfo.CurrentCulture);

    public static RockOcrFields Read(IReadOnlyList<ObservedValue<string>> lines, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(culture);
        return new RockOcrFields
        {
            Mass = Field(lines, "mass", culture),
            Resistance = Field(lines, "resistance", culture),
            Instability = Field(lines, "instability", culture),
            Composition = Shares(lines, culture),
            TotalScu = ReadTotalScu(lines, culture)
        };
    }

    public RockOcrFields WithComposition(IReadOnlyList<ObservedShare> shares, ObservedValue<double> totalScu) =>
        new()
        {
            Mass = Mass,
            Resistance = Resistance,
            Instability = Instability,
            Composition = shares,
            TotalScu = totalScu
        };

    private static IReadOnlyList<ObservedShare> Shares(IReadOnlyList<ObservedValue<string>> lines, CultureInfo culture)
    {
        var shares = new List<ObservedShare>();
        foreach (var line in lines)
        {
            if (line.Value is not string text || IsCrackLabel(text) || CompositionShares.IsInert(text))
            {
                continue;
            }

            if (text.Contains("SCU", StringComparison.OrdinalIgnoreCase) && !text.Contains('%', StringComparison.Ordinal))
            {
                continue;
            }

            if (!NumberText.TryParseFirst(text, culture, out var number))
            {
                continue;
            }

            var name = new string(text.Where(character => char.IsLetter(character) || character is ' ' or '(' or ')').ToArray());
            name = string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            if (name.Length == 0)
            {
                continue;
            }

            var percent = line.IsConfirmed
                ? ObservedValue<double>.Confirmed(number)
                : ObservedValue<double>.Uncertain(number);
            shares.Add(new ObservedShare(name, percent));
        }

        return shares;
    }

    private static ObservedValue<double> ReadTotalScu(IReadOnlyList<ObservedValue<string>> lines, CultureInfo culture)
    {
        foreach (var line in lines)
        {
            if (line.Value is not string text || !TryCompositionScu(text, culture, out var number))
            {
                continue;
            }

            return line.IsConfirmed
                ? ObservedValue<double>.Confirmed(number)
                : ObservedValue<double>.Uncertain(number);
        }

        return ObservedValue<double>.Missing();
    }

    private static bool TryCompositionScu(string text, CultureInfo culture, out double number)
    {
        number = 0;
        var index = text.IndexOf("COMPOSITION", StringComparison.OrdinalIgnoreCase);
        if (index < 0 || !text.Contains("SCU", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var after = text[(index + "COMPOSITION".Length)..];
        var cargo = after.IndexOf("CARGO", StringComparison.OrdinalIgnoreCase);
        if (cargo >= 0)
        {
            after = after[..cargo];
        }

        return NumberText.TryParseFirst(after, culture, out number);
    }

    private static bool IsCrackLabel(string text) =>
        FindLabel(text, "mass") >= 0 || FindLabel(text, "resistance") >= 0 || FindLabel(text, "instability") >= 0;

    private static ObservedValue<double> Field(
        IReadOnlyList<ObservedValue<string>> lines,
        string label,
        CultureInfo culture)
    {
        foreach (var line in lines)
        {
            if (line.Value is not string text)
            {
                continue;
            }

            var index = FindLabel(text, label);
            if (index < 0 || !NumberText.TryParseFirst(text[(index + label.Length)..], culture, out var number))
            {
                continue;
            }

            return line.IsConfirmed
                ? ObservedValue<double>.Confirmed(number)
                : ObservedValue<double>.Uncertain(number);
        }

        return ObservedValue<double>.Missing();
    }

    private static int FindLabel(string text, string label)
    {
        var index = 0;
        while ((index = text.IndexOf(label, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var before = index == 0 || !char.IsLetter(text[index - 1]);
            var after = index + label.Length >= text.Length || !char.IsLetter(text[index + label.Length]);
            if (before && after)
            {
                return index;
            }

            index += label.Length;
        }

        return -1;
    }
}

public sealed record ObservedShare(string Name, ObservedValue<double> Percent);

public static class NumberText
{
    public static bool TryParseFirst(string text, CultureInfo culture, out double value)
    {
        ArgumentNullException.ThrowIfNull(culture);
        foreach (var token in Tokens(text ?? ""))
        {
            if (double.TryParse(token, NumberStyles.Number, culture, out value)
                || double.TryParse(token, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }
        }

        value = 0;
        return false;
    }

    private static IEnumerable<string> Tokens(string text)
    {
        var start = -1;
        for (var i = 0; i <= text.Length; i++)
        {
            var inToken = i < text.Length && IsTokenChar(text[i], start >= 0);
            if (inToken && start < 0)
            {
                start = i;
            }
            else if (!inToken && start >= 0)
            {
                yield return text[start..i].Trim().TrimEnd('.', ',', ' ');
                start = -1;
            }
        }
    }

    private static bool IsTokenChar(char character, bool started) =>
        char.IsDigit(character)
        || character is '.' or ',' or ' ' or '\u00A0'
        || (!started && character == '-');
}
