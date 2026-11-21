namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// The composition list under the scan-results panel.
/// Same text column as <see cref="RockPatch"/>. The measured scan-results still
/// ends on the COMPOSITION header (fixture rock-panel-5380-live), so this window
/// overlaps that header by 4 percent of the screen and continues below it for the
/// share rows. The height is the scan-results window. A miss uses the same step.
/// </summary>
public static class CompositionPatch
{
    public const double X = RockPatch.X;

    public const double Y = RockPatch.Y + RockPatch.HeightFraction - 0.04;

    public const double WidthFraction = RockPatch.WidthFraction;

    public const double HeightFraction = RockPatch.HeightFraction;

    public static ScanPatch.Rect Of(int screenWidth, int screenHeight) =>
        new(
            (int)(screenWidth * X),
            (int)(screenHeight * Y),
            Math.Max(8, (int)(screenWidth * WidthFraction)),
            Math.Max(8, (int)(screenHeight * HeightFraction)));

    public static ScanPatch.Rect? Shifted(int screenWidth, int screenHeight, int offsetX, int offsetY)
    {
        var patch = Of(screenWidth, screenHeight);
        var x = patch.X + offsetX;
        var y = patch.Y + offsetY;
        if (x < 0 || y < 0 || x + patch.Width > screenWidth || y + patch.Height > screenHeight)
        {
            return null;
        }

        return patch with { X = x, Y = y };
    }
}
