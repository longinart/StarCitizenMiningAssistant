namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// The fixed screen patch that holds the scan readout under the radar.
/// Fractions of the monitor, the same place on each cockpit. Decision 021.
/// </summary>
public static class ScanPatch
{
    public const double X = 0.38;
    public const double Y = 0.28;
    public const double WidthFraction = 0.24;
    public const double HeightFraction = 0.12;
    public const double DigitHeightAt1600 = 11;

    public readonly record struct Rect(int X, int Y, int Width, int Height);

    public static Rect Of(int screenWidth, int screenHeight) =>
        new(
            (int)(screenWidth * X),
            (int)(screenHeight * Y),
            (int)(screenWidth * WidthFraction),
            (int)(screenHeight * HeightFraction));

    public static double DigitHeight(int screenHeight) => DigitHeightAt1600 * screenHeight / 1600.0;

    /// <summary>About 100 px on a 1440 px tall screen. One step when the number has left the patch.</summary>
    public static int Step(int screenHeight) => Math.Max(40, 100 * screenHeight / 1440);

    public static Rect? Shifted(int screenWidth, int screenHeight, int offsetX, int offsetY)
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
