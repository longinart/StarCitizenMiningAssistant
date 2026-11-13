using System.Globalization;

namespace StarCitizenMiningAssistant.Core.Recognition;

public sealed class RockOcrFields
{
    public required ObservedValue<double> Mass { get; init; }

    public required ObservedValue<double> Resistance { get; init; }

    public required ObservedValue<double> Instability { get; init; }

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
            Instability = Field(lines, "instability", culture)
        };
    }

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
