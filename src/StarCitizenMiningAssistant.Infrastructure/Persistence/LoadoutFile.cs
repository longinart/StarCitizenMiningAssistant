using System.Text.Json;

namespace StarCitizenMiningAssistant.Infrastructure.Persistence;

public sealed class SavedLoadout
{
    public string ShipId { get; set; } = "";

    public string? GadgetId { get; set; }

    public string MassText { get; set; } = "5000";

    public string ResistanceText { get; set; } = "0";

    public string InstabilityText { get; set; } = "";

    public string SignatureText { get; set; } = "";

    public List<SavedTurret> Turrets { get; set; } = [];

    public List<string> TrackedMinerals { get; set; } = [];
}

public sealed class SavedTurret
{
    public string? HeadId { get; set; }

    public bool LaserOn { get; set; }

    public string CraftPercent { get; set; } = "0";

    public List<string?> Modules { get; set; } = [];
}

public static class LoadoutFile
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static string DefaultPath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "StarCitizenMiningAssistant", "loadout.json");
    }

    public static SavedLoadout? TryLoad(string? path = null)
    {
        var file = path ?? DefaultPath();
        try
        {
            if (!File.Exists(file))
            {
                return null;
            }

            return JsonSerializer.Deserialize<SavedLoadout>(File.ReadAllText(file), JsonOptions);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    public static void Save(SavedLoadout loadout, string? path = null)
    {
        ArgumentNullException.ThrowIfNull(loadout);
        var file = path ?? DefaultPath();
        var directory = Path.GetDirectoryName(file);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(file, JsonSerializer.Serialize(loadout, JsonOptions));
    }
}
