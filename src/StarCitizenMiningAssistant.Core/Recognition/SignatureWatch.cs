using StarCitizenMiningAssistant.Core.Equipment;

namespace StarCitizenMiningAssistant.Core.Recognition;

public sealed record SignatureWatchHit(string Mineral, int DepositNumber, bool Blink)
{
    public string Label => DepositNumber > 0 ? DepositNumber + " × " + Mineral : Mineral;
}

public sealed class SignatureWatchResult
{
    public static readonly SignatureWatchResult None = new([], false);

    public SignatureWatchResult(IReadOnlyList<SignatureWatchHit> hits, bool playSound)
    {
        Hits = hits;
        PlaySound = playSound;
    }

    public IReadOnlyList<SignatureWatchHit> Hits { get; }

    public bool PlaySound { get; }
}

/// <summary>
/// Matches one confirmed signature to the catalog. An uncertain reading does not
/// match, blink, or request a sound.
/// </summary>
public static class SignatureWatch
{
    public static SignatureWatchResult Evaluate(
        MiningCatalog catalog,
        ObservedValue<double> signature,
        IEnumerable<string>? trackedMinerals,
        bool soundEnabled,
        ScanSignatureChart? chart = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (!signature.IsConfirmed)
        {
            return SignatureWatchResult.None;
        }

        var tracked = new HashSet<string>(trackedMinerals ?? [], StringComparer.OrdinalIgnoreCase);
        var rounded = (int)Math.Round(signature.Value);
        var hits = Identify(catalog, chart, rounded, tracked);
        var playSound = soundEnabled && hits.Exists(hit => hit.Blink);
        return new SignatureWatchResult(hits, playSound);
    }

    private static List<SignatureWatchHit> Identify(
        MiningCatalog catalog,
        ScanSignatureChart? chart,
        int value,
        HashSet<string> tracked)
    {
        var ores = catalog.MatchSignature(value)
            .Where(match => !IsHandOrRocRock(match.Material))
            .ToList();
        if (ores.Count == 1)
        {
            var match = ores[0];
            return
            [
                new SignatureWatchHit(match.Material.Name, match.DepositNumber, tracked.Contains(match.Material.Name))
            ];
        }

        if (ores.Count > 1)
        {
            return [];
        }

        var fromChart = chart?.Matches(value).Where(match => !SignatureGroups.IsChartGroup(match.Kind)).ToList() ?? [];
        if (fromChart.Count == 1)
        {
            var match = fromChart[0];
            var name = Title(match.Kind);
            return [new SignatureWatchHit(name, match.Count, tracked.Contains(name))];
        }

        if (fromChart.Count > 1)
        {
            return [];
        }

        var hand = SignatureGroups.Multiple(value, SignatureGroups.HandAndRocBase);
        var salvage = SignatureGroups.Multiple(value, SignatureGroups.SalvageBase);
        if (hand is int handCount && salvage is null && tracked.Contains(SignatureGroups.HandAndRoc))
        {
            return [new SignatureWatchHit(SignatureGroups.HandAndRoc, handCount, true)];
        }

        if (salvage is int salvageCount && hand is null && tracked.Contains(SignatureGroups.Salvage))
        {
            return [new SignatureWatchHit(SignatureGroups.Salvage, salvageCount, true)];
        }

        return [];
    }

    private static bool IsHandOrRocRock(MineableMaterial material) =>
        material.Category.Equals("fps", StringComparison.OrdinalIgnoreCase)
        || material.Category.Equals("vehicle", StringComparison.OrdinalIgnoreCase);

    private static string Title(string kind) =>
        kind.Length == 0 ? kind : char.ToUpperInvariant(kind[0]) + kind[1..];
}
