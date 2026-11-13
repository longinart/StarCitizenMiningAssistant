namespace StarCitizenMiningAssistant.Infrastructure.Capture;

public sealed class MonitorInfo
{
    public MonitorInfo(string deviceName, int left, int top, int width, int height, bool isPrimary)
    {
        DeviceName = deviceName;
        Left = left;
        Top = top;
        Width = width;
        Height = height;
        IsPrimary = isPrimary;
    }

    public string DeviceName { get; }

    public int Left { get; }

    public int Top { get; }

    public int Width { get; }

    public int Height { get; }

    public bool IsPrimary { get; }

    public string Name =>
        DeviceName + "   " + Width + " × " + Height + (IsPrimary ? "   primary" : "");
}
