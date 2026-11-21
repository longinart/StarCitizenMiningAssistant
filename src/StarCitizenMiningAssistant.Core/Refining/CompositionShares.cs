namespace StarCitizenMiningAssistant.Core.Refining;

public static class CompositionShares
{
    public static bool Fits(IEnumerable<double> percentages)
    {
        ArgumentNullException.ThrowIfNull(percentages);
        decimal sum = 0;
        foreach (var value in percentages)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                continue;
            }

            sum += (decimal)value;
        }

        return sum <= 100m;
    }

    public static bool IsInert(string? text) =>
        text?.Contains("INERT", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// A catalog mineral, or null when the reading is not close to one name on the list.
    /// An exact token wins. A reading of four letters that begins exactly one name wins.
    /// Otherwise the closest name wins at 60 percent or above, and only when the next name is at least 8 points behind.
    /// </summary>
    public static string? DisplayName(string read, IReadOnlyList<string> catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var cleaned = Strip(read);
        var exact = Resolve(cleaned, catalog);
        if (exact is not null)
        {
            return exact;
        }

        string? tokenHit = null;
        foreach (var token in cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var hit = Resolve(token, catalog);
            if (hit is not null && (tokenHit is null || hit.Length > tokenHit.Length))
            {
                tokenHit = hit;
            }
        }

        if (tokenHit is not null)
        {
            return tokenHit;
        }

        var letters = Letters(cleaned);
        if (letters.Length >= 4)
        {
            string? prefix = null;
            var count = 0;
            foreach (var name in catalog)
            {
                if (Letters(name).StartsWith(letters, StringComparison.Ordinal))
                {
                    count++;
                    prefix = name;
                }
            }

            if (count == 1)
            {
                return prefix;
            }
        }

        return Closest(letters, catalog);
    }

    private static string? Find(IReadOnlyList<string> catalog, string wanted)
    {
        foreach (var name in catalog)
        {
            if (name.Equals(wanted, StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }
        }

        return null;
    }

    private static string Strip(string read)
    {
        var text = read;
        foreach (var suffix in new[] { "(ORE)", "(RAW)", "(PURE)", "(R)" })
        {
            text = text.Replace(suffix, "", StringComparison.OrdinalIgnoreCase);
        }

        var parts = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1 && IsStateWord(parts[0]))
        {
            parts = parts[1..];
        }

        if (parts.Length > 1 && IsStateWord(parts[^1]))
        {
            parts = parts[..^1];
        }

        return string.Join(' ', parts);
    }

    private static bool IsStateWord(string word) =>
        word.Equals("ORE", StringComparison.OrdinalIgnoreCase)
        || word.Equals("RAW", StringComparison.OrdinalIgnoreCase)
        || word.Equals("PURE", StringComparison.OrdinalIgnoreCase);

    private static string? Resolve(string text, IReadOnlyList<string> catalog)
    {
        var exact = Find(catalog, text);
        if (exact is not null)
        {
            return exact;
        }

        foreach (var alias in Aliases(text))
        {
            exact = Find(catalog, alias);
            if (exact is not null)
            {
                return exact;
            }
        }

        return null;
    }

    private static string? Closest(string letters, IReadOnlyList<string> catalog)
    {
        if (letters.Length < 4)
        {
            return null;
        }

        string? bestName = null;
        var best = 0d;
        var second = 0d;
        foreach (var name in catalog)
        {
            var score = Similarity(letters, Letters(name));
            if (score > best)
            {
                second = best;
                best = score;
                bestName = name;
            }
            else if (score > second)
            {
                second = score;
            }
        }

        return best >= 0.60 && best - second >= 0.08 ? bestName : null;
    }

    private static double Similarity(string left, string right)
    {
        if (left.Length == 0 || right.Length == 0)
        {
            return 0;
        }

        var distance = EditDistance(left, right);
        return 1d - (distance / (double)Math.Max(left.Length, right.Length));
    }

    private static int EditDistance(string left, string right)
    {
        var previous = new int[right.Length + 1];
        for (var column = 0; column <= right.Length; column++)
        {
            previous[column] = column;
        }

        for (var row = 1; row <= left.Length; row++)
        {
            var current = new int[right.Length + 1];
            current[0] = row;
            for (var column = 1; column <= right.Length; column++)
            {
                var cost = left[row - 1] == right[column - 1] ? 0 : 1;
                current[column] = Math.Min(
                    Math.Min(current[column - 1] + 1, previous[column] + 1),
                    previous[column - 1] + cost);
            }

            previous = current;
        }

        return previous[right.Length];
    }

    private static string Letters(string text)
    {
        var letters = new char[text.Length];
        var count = 0;
        foreach (var character in text)
        {
            if (char.IsLetter(character))
            {
                letters[count++] = char.ToUpperInvariant(character);
            }
        }

        return new string(letters, 0, count);
    }

    private static IEnumerable<string> Aliases(string name)
    {
        if (name.Equals("Aluminium", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Aluminum";
        }
        else if (name.Equals("Aluminum", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Aluminium";
        }
        else if (name.Equals("Silicone", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Silicon";
        }
        else if (name.Equals("Outarite", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Ouratite";
        }
    }
}
