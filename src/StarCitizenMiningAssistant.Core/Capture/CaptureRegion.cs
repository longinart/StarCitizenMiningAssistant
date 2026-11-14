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
}

public sealed class MonitorCapture
{
    public string DeviceName { get; set; } = "";

    public CaptureRectangle? Signatures { get; set; }

    public CaptureRectangle? Rock { get; set; }
}
