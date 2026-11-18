using System.Globalization;

namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// Mass, resistance, and instability from one scan-results patch.
/// The longer of the word beside the label and the digit shapes is kept.
/// A shorter fragment is left blank when the row had more digits. MASS is also accepted
/// when the reader saw HASS or HAS. A missing mass or instability label still takes
/// the number one row above or below resistance. Composition is ignored.
/// </summary>
public static class RockPanel
{
    public readonly record struct Word(string Text, int X, int Y, int Height);

    public static RockOcrFields Read(
        IReadOnlyList<Word> words,
        IReadOnlyList<RockDigitReader.Amount> amounts)
    {
        ArgumentNullException.ThrowIfNull(words);
        ArgumentNullException.ThrowIfNull(amounts);
        var massLabel = Find(words, IsMass);
        var resistanceLabel = Find(words, IsResistance);
        var instabilityLabel = Find(words, IsInstability);
        var mass = ValueAt(massLabel, words, amounts);
        var resistance = ValueAt(resistanceLabel, words, amounts);
        var instability = ValueAt(instabilityLabel, words, amounts);
        if (mass.Confidence == RecognitionConfidence.Missing && resistanceLabel is Word above)
        {
            mass = NumberAt(words, amounts, above.Y - 24, above.X);
        }

        if (instability.Confidence == RecognitionConfidence.Missing && resistanceLabel is Word below)
        {
            instability = NumberAt(words, amounts, below.Y + 24, below.X);
        }

        return new RockOcrFields
        {
            Mass = mass,
            Resistance = resistance,
            Instability = instability
        };
    }

    private static Word? Find(IReadOnlyList<Word> words, Func<string, bool> label)
    {
        foreach (var word in words)
        {
            if (label(word.Text))
            {
                return word;
            }
        }

        return null;
    }

    private static ObservedValue<double> ValueAt(
        Word? label,
        IReadOnlyList<Word> words,
        IReadOnlyList<RockDigitReader.Amount> amounts) =>
        label is Word anchor
            ? NumberAt(words, amounts, anchor.Y, anchor.X)
            : ObservedValue<double>.Missing();

    private static ObservedValue<double> NumberAt(
        IReadOnlyList<Word> words,
        IReadOnlyList<RockDigitReader.Amount> amounts,
        int y,
        int minX)
    {
        var beside = words
            .Where(word => word.X > minX + 8 && Math.Abs(word.Y - y) <= 14)
            .OrderBy(word => Math.Abs(word.Y - y))
            .ThenBy(word => word.X)
            .Select(word => Digits(word.Text))
            .FirstOrDefault(text => text is not null);
        var near = amounts
            .Where(amount => amount.X > minX + 8 && Math.Abs(amount.Y - y) <= 14)
            .ToList();
        var shaped = near
            .Where(amount => amount.Text.Length > 0)
            .OrderByDescending(amount => DigitCount(amount.Text))
            .ThenBy(amount => Math.Abs(amount.Y - y))
            .ThenBy(amount => amount.X)
            .Select(amount => amount.Text)
            .FirstOrDefault(text => double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out _));
        var seen = near.Count == 0
            ? 0
            : near.Max(amount => Math.Max(amount.Digits, DigitCount(amount.Text)));
        if (beside is not null && shaped is not null && DigitCount(shaped) > DigitCount(beside)
            && double.TryParse(shaped, NumberStyles.Number, CultureInfo.InvariantCulture, out var longer))
        {
            return ObservedValue<double>.Uncertain(longer);
        }

        if (beside is not null && shaped is not null && CorrectsZero(beside, shaped)
            && double.TryParse(shaped, NumberStyles.Number, CultureInfo.InvariantCulture, out var corrected))
        {
            return ObservedValue<double>.Uncertain(corrected);
        }

        if (beside is not null && DigitCount(beside) >= seen
            && double.TryParse(beside, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return ObservedValue<double>.Uncertain(parsed);
        }

        return shaped is not null && double.TryParse(shaped, NumberStyles.Number, CultureInfo.InvariantCulture, out var fromShape)
            ? ObservedValue<double>.Uncertain(fromShape)
            : ObservedValue<double>.Missing();
    }

    private static int DigitCount(string? text) => text is null ? 0 : text.Count(char.IsDigit);

    /// <summary>
    /// The glyph reader saw an 8 where the text reader saw a 0. A bar with a hole on each side is the deciding mark.
    /// </summary>
    private static bool CorrectsZero(string text, string shaped)
    {
        if (text.Length != shaped.Length)
        {
            return false;
        }

        var changed = false;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == shaped[i])
            {
                continue;
            }

            if (text[i] == '0' && shaped[i] == '8')
            {
                changed = true;
                continue;
            }

            return false;
        }

        return changed;
    }

    private static bool IsMass(string text)
    {
        var letters = Letters(text);
        return letters is "MASS" or "HASS" or "HAS" or "MAS";
    }

    private static bool IsResistance(string text) => Letters(text).Contains("RESIST", StringComparison.Ordinal);

    private static bool IsInstability(string text) => Letters(text).Contains("STABIL", StringComparison.Ordinal);

    private static string Letters(string text)
    {
        var letters = new string(text.Where(char.IsLetter).ToArray());
        return letters.ToUpperInvariant();
    }

    private static string? Digits(string text)
    {
        var cleaned = text.Trim().TrimEnd('%').Replace(" ", "", StringComparison.Ordinal);
        if (cleaned.Length == 0)
        {
            return null;
        }

        foreach (var character in cleaned)
        {
            if (!char.IsDigit(character) && character != '.')
            {
                return null;
            }
        }

        return cleaned.Count(character => character == '.') > 1 ? null : cleaned;
    }
}
