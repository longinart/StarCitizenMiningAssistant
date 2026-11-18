namespace StarCitizenMiningAssistant.Core.Capture;

public enum CaptureTarget
{
    Signatures,
    Rock
}

/// <summary>
/// A rectangle the user placed, in pixels relative to one monitor.
/// </summary>
public sealed class CaptureRectangle
{
    public double Left { get; set; }

    public double Top { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    public bool IsPlaceable =>
        Width >= 8
        && Height >= 8
        && double.IsFinite(Left)
        && double.IsFinite(Top)
        && double.IsFinite(Width)
        && double.IsFinite(Height);

    /// <summary>
    /// The same rectangle moved by one step. Null when that step would leave the monitor.
    /// The placed rectangle is left where the user drew it.
    /// </summary>
    public CaptureRectangle? Shifted(int screenWidth, int screenHeight, int offsetX, int offsetY)
    {
        if (!IsPlaceable || screenWidth < 1 || screenHeight < 1)
        {
            return null;
        }

        var left = (int)Math.Round(Left) + offsetX;
        var top = (int)Math.Round(Top) + offsetY;
        var width = (int)Math.Round(Width);
        var height = (int)Math.Round(Height);
        if (left < 0 || top < 0 || left + width > screenWidth || top + height > screenHeight)
        {
            return null;
        }

        return new CaptureRectangle
        {
            Left = left,
            Top = top,
            Width = width,
            Height = height
        };
    }
}

public sealed class MonitorCapture
{
    public string DeviceName { get; set; } = "";

    public CaptureRectangle? Signatures { get; set; }

    public CaptureRectangle? Rock { get; set; }
}
