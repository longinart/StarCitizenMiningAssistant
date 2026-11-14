using StarCitizenMiningAssistant.Core.Equipment;

namespace StarCitizenMiningAssistant.Infrastructure.Persistence;

public static class LoadoutFile
{
    public static string DefaultPath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "StarCitizenMiningAssistant", "loadout.json");
    }

    public static LoadoutLibrary? TryLoad(string? path = null)
    {
        var file = path ?? DefaultPath();
        try
        {
            if (!File.Exists(file))
            {
                return null;
            }

            return LoadoutLibrary.TryParse(File.ReadAllText(file));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static void Save(LoadoutLibrary library, string? path = null)
    {
        ArgumentNullException.ThrowIfNull(library);
        var file = path ?? DefaultPath();
        var directory = Path.GetDirectoryName(file);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(file, library.ToJson());
    }
}
