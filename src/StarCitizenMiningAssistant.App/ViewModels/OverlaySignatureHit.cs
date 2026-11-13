using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.App.ViewModels;

public sealed class OverlaySignatureHit
{
    private OverlaySignatureHit(string name, string detail, bool blink)
    {
        Name = name;
        Detail = detail;
        Blink = blink;
        HasDetail = detail.Length > 0;
    }

    public string Name { get; }

    public string Detail { get; }

    public bool Blink { get; }

    public bool HasDetail { get; }

    public static OverlaySignatureHit From(SignatureWatchHit hit) =>
        new(hit.Mineral, hit.DepositNumber > 0 ? "deposit " + hit.DepositNumber : "", hit.Blink);
}
