namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// The fixed patch that holds the scan-results panel: mass, resistance, instability.
/// Measured on 52 cockpit screenshots at 2560×1440. The header sat between
/// y 614 and y 754, and the text column stayed near x 1720–1980.
/// The window is tall enough that a header at the bottom of the measured range
/// still has the three number rows inside it. One 100 px step reaches past that. Decision 022.
/// </summary>
public static class RockPatch
{
    public const double X = 0.64;
    public const double Y = 0.41;
    public const double WidthFraction = 0.18;
    public const double HeightFraction = 0.20;

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
