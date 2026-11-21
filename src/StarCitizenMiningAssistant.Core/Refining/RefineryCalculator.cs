namespace StarCitizenMiningAssistant.Core.Refining;

public readonly record struct RefineryRow(string Name, double Percentage, bool Confirmed);

public static class RefineryCalculator
{
    public static RockComposition Calculate(
        double totalScu,
        string? methodName,
        string? homeRefinery,
        IReadOnlyList<RefineryRow> rows,
        RefineryCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(catalog);
        var method = catalog.Methods.FirstOrDefault(item =>
            item.Name.Equals(methodName?.Trim() ?? "", StringComparison.OrdinalIgnoreCase));
        var station = catalog.Refineries.FirstOrDefault(item =>
            item.Name.Equals(homeRefinery?.Trim() ?? "", StringComparison.OrdinalIgnoreCase));
        var components = new List<MineralComponent>();
        if (totalScu < 0 || double.IsNaN(totalScu) || double.IsInfinity(totalScu))
        {
            totalScu = 0;
        }

        foreach (var row in rows)
        {
            if (!row.Confirmed || string.IsNullOrWhiteSpace(row.Name) || row.Percentage <= 0 || double.IsNaN(row.Percentage))
            {
                continue;
            }

            var raw = (decimal)totalScu * (decimal)row.Percentage / 100m;
            double? refined = null;
            if (method is not null)
            {
                var bonus = StationPercent(station, row.Name);
                var value = raw * (decimal)method.YieldPercent / 100m * (1m + (decimal)bonus / 100m);
                refined = (double)value;
            }

            components.Add(new MineralComponent(row.Name.Trim(), row.Percentage, (double)raw, refined));
        }

        return new RockComposition(totalScu, components);
    }

    private static double StationPercent(RefineryStation? station, string mineral)
    {
        if (station is null)
        {
            return 0;
        }

        foreach (var key in Names(mineral))
        {
            if (station.Bonuses.TryGetValue(key, out var percent))
            {
                return percent;
            }
        }

        return 0;
    }

    internal static IEnumerable<string> Names(string mineral)
    {
        var trimmed = mineral.Trim();
        yield return trimmed;
        if (trimmed.Equals("Aluminium", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Aluminum";
        }
        else if (trimmed.Equals("Aluminum", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Aluminium";
        }
        else if (trimmed.Equals("Silicone", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Silicon";
        }
        else if (trimmed.Equals("Outarite", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Ouratite";
        }
    }
}
