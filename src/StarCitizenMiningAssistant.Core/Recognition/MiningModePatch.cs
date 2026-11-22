namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// The HUD phrase above the mining dial: FRACTURE MODE or EXTRACTION MODE.
/// Measured on 2560×1440 cockpit shots from 2026-11-14 and 2026-11-15.
/// The green line sat between x 1191–1339 and y 450–572. The window keeps a margin
/// around that line. It does not follow the scan-results panel.
/// </summary>
public static class MiningModePatch
{
    public const double X = 0.42;

    public const double Y = 0.26;

    public const double WidthFraction = 0.16;

    public const double HeightFraction = 0.20;

    public static ScanPatch.Rect Of(int screenWidth, int screenHeight) =>
        new(
            (int)(screenWidth * X),
            (int)(screenHeight * Y),
            Math.Max(8, (int)(screenWidth * WidthFraction)),
            Math.Max(8, (int)(screenHeight * HeightFraction)));
}
