using StarCitizenMiningAssistant.Core.Equipment;

namespace StarCitizenMiningAssistant.Core.Recognition;

public sealed record SignatureWatchHit(string Mineral, int DepositNumber, bool Blink);

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
        bool soundEnabled)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (!signature.IsConfirmed)
        {
            return SignatureWatchResult.None;
        }

        var tracked = new HashSet<string>(trackedMinerals ?? [], StringComparer.OrdinalIgnoreCase);
        var hits = catalog.MatchSignature(signature.Value).Select(match => new SignatureWatchHit(
            match.Material.Name,
            match.DepositNumber,
            tracked.Contains(match.Material.Name))).ToList();
        var playSound = soundEnabled && hits.Exists(hit => hit.Blink);
        return new SignatureWatchResult(hits, playSound);
    }
}
