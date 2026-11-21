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

    public static string DisplayName(string read, IReadOnlyList<string> catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var cleaned = Strip(read);
        var exact = Find(catalog, cleaned);
        if (exact is not null)
        {
            return exact;
        }

        foreach (var alias in Aliases(cleaned))
        {
            exact = Find(catalog, alias);
            if (exact is not null)
            {
                return exact;
            }
        }

        return cleaned;
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
        foreach (var suffix in new[] { "(ORE)", "(RAW)", "(PURE)" })
        {
            text = text.Replace(suffix, "", StringComparison.OrdinalIgnoreCase);
        }

        return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
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
