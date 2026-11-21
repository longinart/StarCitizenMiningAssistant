using StarCitizenMiningAssistant.Core.Localization;

namespace StarCitizenMiningAssistant.Core.Tests;

public class MineralShortcutTests
{
    [Fact]
    public void An_exact_key_changes_and_a_description_stays()
    {
        var text = "items_commodities_tin_ore=Tin (Ore)\r\nitems_commodities_tin_ore_desc=keep this\r\n";
        var names = new Dictionary<string, string>
        {
            ["items_commodities_tin_ore"] = "Tin",
            ["items_commodities_hephaestanite"] = "Hepha"
        };

        var patched = MineralShortcuts.Apply(text, names);

        Assert.Contains("items_commodities_tin_ore=Tin\r\n", patched);
        Assert.Contains("items_commodities_tin_ore_desc=keep this", patched);
        Assert.DoesNotContain("Tin (Ore)", patched);
        Assert.EndsWith("items_commodities_hephaestanite=Hepha\r\n", patched);
    }

    [Fact]
    public void The_shipped_table_shortens_hephaestanite_and_quantainium()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "data", "mineral-shortcuts.json")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        var names = MineralShortcuts.Parse(File.ReadAllText(Path.Combine(directory.FullName, "data", "mineral-shortcuts.json")));
        Assert.Equal(36, names.Count);
        Assert.Equal("Hepha", names["items_commodities_hephaestanite"]);
        Assert.Equal("Hepha", names["items_commodities_hephaestanite_raw"]);
        Assert.Equal("Quantanium", names["items_commodities_quantainium_raw"]);
        Assert.Equal("Aluminium", names["items_commodities_aluminum_ore"]);
    }
}
