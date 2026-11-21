namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// The signature overlay shows a confirmed line once, then hides.
/// The same line does not show again.
/// </summary>
public static class SignatureOverlayNotice
{
    public static readonly TimeSpan VisibleFor = TimeSpan.FromSeconds(10);

    public static bool IsNewResult(string? shown, string? candidate)
    {
        var next = candidate?.Trim() ?? "";
        return next.Length > 0 && !string.Equals(shown?.Trim(), next, StringComparison.Ordinal);
    }
}
