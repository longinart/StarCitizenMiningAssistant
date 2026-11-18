namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// Where the last signature was found. The next frame looks there first,
/// then at the radar patch, then one step up, down, left, and right.
/// </summary>
public sealed class SignatureAim
{
    public int X { get; private set; }

    public int Y { get; private set; }

    public IReadOnlyList<(int X, int Y)> Order(int screenHeight)
    {
        var step = ScanPatch.Step(screenHeight);
        (int X, int Y)[] neighbors = [(0, -step), (0, step), (-step, 0), (step, 0)];
        var order = new List<(int X, int Y)>();
        if (X != 0 || Y != 0)
        {
            order.Add((X, Y));
        }

        if (!order.Contains((0, 0)))
        {
            order.Add((0, 0));
        }

        foreach (var neighbor in neighbors)
        {
            if (!order.Contains(neighbor))
            {
                order.Add(neighbor);
            }
        }

        return order;
    }

    public void Remember(int x, int y)
    {
        X = x;
        Y = y;
    }
}
