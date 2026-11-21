namespace StarCitizenMiningAssistant.Core.Refining;

public sealed record SellQuote(string Terminal, long Auec);

public sealed record CommodityQuote(
    long? RawAuec,
    long? RefinedAuec,
    IReadOnlyList<SellQuote> TopRaw,
    IReadOnlyList<SellQuote> TopRefined);

public static class CommodityPricing
{
    public static CommodityQuote Quote(RockComposition composition, RefineryCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(catalog);
        var raw = Rank(composition, catalog, "raw", component => component.RawScu);
        var refined = Rank(composition, catalog, "refined", component => component.RefinedScu ?? 0);
        return new CommodityQuote(raw.Best, refined.Best, raw.Top, refined.Top);
    }

    private static (long? Best, IReadOnlyList<SellQuote> Top) Rank(
        RockComposition composition,
        RefineryCatalog catalog,
        string kind,
        Func<MineralComponent, double> scu)
    {
        var parts = composition.Components
            .Select(component => (component, amount: scu(component)))
            .Where(part => part.amount > 0)
            .ToList();
        if (parts.Count == 0)
        {
            return (null, []);
        }

        decimal combined = 0;
        var priced = false;
        var totals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var (component, amount) in parts)
        {
            long? best = null;
            foreach (var sell in catalog.Sells)
            {
                if (!sell.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase) || !SameMineral(sell.Mineral, component.Name))
                {
                    continue;
                }

                totals.TryGetValue(sell.Terminal, out var soFar);
                totals[sell.Terminal] = soFar + ((decimal)amount * sell.Price);
                if (best is null || sell.Price > best)
                {
                    best = sell.Price;
                }
            }

            if (best is long price)
            {
                combined += (decimal)amount * price;
                priced = true;
            }
        }

        var ranked = totals
            .Where(pair => pair.Value > 0)
            .Select(pair => new SellQuote(pair.Key, (long)Math.Round(pair.Value, MidpointRounding.AwayFromZero)))
            .OrderByDescending(quote => quote.Auec)
            .ThenBy(quote => quote.Terminal, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var headline = priced ? (long)Math.Round(combined, MidpointRounding.AwayFromZero) : (long?)null;
        return (headline, ranked.Take(3).ToList());
    }

    private static bool SameMineral(string stored, string typed)
    {
        foreach (var name in RefineryCalculator.Names(typed))
        {
            if (stored.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
