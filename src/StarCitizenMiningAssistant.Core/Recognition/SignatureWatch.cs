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
        var decoded = chart?.Matches((int)Math.Round(signature.Value)) ?? [];
        var hits = decoded.Count > 0
            ? decoded.Select(match => new SignatureWatchHit(
                Title(match.Kind),
                match.Count,
                tracked.Contains(Title(match.Kind)))).ToList()
            : catalog.MatchSignature(signature.Value).Select(match => new SignatureWatchHit(
                match.Material.Name,
                match.DepositNumber,
                tracked.Contains(match.Material.Name))).ToList();
        var playSound = soundEnabled && hits.Exists(hit => hit.Blink);
        return new SignatureWatchResult(hits, playSound);
    }

    private static string Title(string kind) =>
        kind.Length == 0 ? kind : char.ToUpperInvariant(kind[0]) + kind[1..];
}
