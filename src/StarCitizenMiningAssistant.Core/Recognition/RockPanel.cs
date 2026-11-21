using System.Globalization;
using StarCitizenMiningAssistant.Core.Refining;

namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// Mass, resistance, and instability from one scan-results patch.
/// The longer of the word beside the label and the digit shapes is kept.
/// A shorter fragment is left blank when the row had more digits. MASS is also accepted
/// when the reader saw HASS or HAS. A missing mass or instability label still takes
/// the number one row above or below resistance. A mineral name on the same row
/// as a percent is kept as an uncertain composition share. The live row puts the percent first.
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
            Instability = instability,
            Composition = Shares(words),
            TotalScu = ReadTotalScu(words)
        };
    }

    private static IReadOnlyList<ObservedShare> Shares(IReadOnlyList<Word> words)
    {
        var shares = new List<ObservedShare>();
        foreach (var percent in words)
        {
            if (Digits(percent.Text) is not string number || !percent.Text.Contains('%', StringComparison.Ordinal))
            {
                continue;
            }

            if (words.Any(word => IsCrack(word.Text) && Math.Abs(word.Y - percent.Y) <= 14 && word.X < percent.X))
            {
                continue;
            }

            var name = NameBeside(words, percent);
            if (name is null || CompositionShares.IsInert(name)
                || !double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                continue;
            }

            shares.Add(new ObservedShare(name, ObservedValue<double>.Uncertain(value)));
        }

        return shares;
    }

    private static ObservedValue<double> ReadTotalScu(IReadOnlyList<Word> words)
    {
        foreach (var header in words)
        {
            if (!Letters(header.Text).Contains("COMPOSITION", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var word in words.Where(word => word.X > header.X && Math.Abs(word.Y - header.Y) <= 14).OrderBy(word => word.X))
            {
                if (Letters(word.Text).Contains("CARGO", StringComparison.Ordinal))
                {
                    break;
                }

                if (TryAmount(word.Text, out var amount))
                {
                    return ObservedValue<double>.Uncertain(amount);
                }
            }
        }

        return ObservedValue<double>.Missing();
    }

    private static bool TryAmount(string text, out double value)
    {
        var start = -1;
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsDigit(text[i]) || text[i] == '.')
            {
                if (start < 0)
                {
                    start = i;
                }

                continue;
            }

            if (start >= 0)
            {
                break;
            }
        }

        if (start < 0)
        {
            value = 0;
            return false;
        }

        var end = start;
        while (end < text.Length && (char.IsDigit(text[end]) || text[end] == '.'))
        {
            end++;
        }

        return double.TryParse(text[start..end], NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// The live row is the percent, then the mineral, then a signature number.
    /// A name to the left of the percent still counts.
    /// </summary>
    private static string? NameBeside(IReadOnlyList<Word> words, Word percent)
    {
        var right = new List<string>();
        foreach (var word in words.Where(word => word.X > percent.X && Math.Abs(word.Y - percent.Y) <= 8).OrderBy(word => word.X))
        {
            if (word.Text.Any(char.IsDigit) || IsCrack(word.Text) || IsChrome(word.Text))
            {
                break;
            }

            if (Letters(word.Text).Length > 1)
            {
                right.Add(word.Text.Trim());
            }
        }

        if (right.Any(part => !IsState(part)))
        {
            return string.Join(' ', right);
        }

        return words
            .Where(word => Math.Abs(word.Y - percent.Y) <= 14 && word.X < percent.X && HasLetters(word.Text) && !IsCrack(word.Text) && !IsChrome(word.Text))
            .OrderByDescending(word => word.X)
            .Select(word => Letters(word.Text))
            .FirstOrDefault(text => text.Length > 1);
    }

    private static bool IsState(string text)
    {
        var letters = Letters(text);
        return letters is "ORE" or "RAW" or "PURE";
    }

    private static bool IsCrack(string text) => IsMass(text) || IsResistance(text) || IsInstability(text);

    private static bool IsChrome(string text)
    {
        var letters = Letters(text);
        return letters.Contains("COMPOSITION", StringComparison.Ordinal)
            || letters.Contains("CARGO", StringComparison.Ordinal)
            || letters.Contains("SCU", StringComparison.Ordinal);
    }

    private static bool HasLetters(string text) => text.Any(char.IsLetter);

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
