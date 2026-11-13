using StarCitizenMiningAssistant.Core.Calculation;
using StarCitizenMiningAssistant.Core.Equipment;
using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.Core.Tests;

public class LoadoutComparisonTests
{
    [Fact]
    public void A_missing_mass_is_unknown_for_every_loadout()
    {
        var catalog = Catalog();
        var rock = new RockReading(
            ObservedValue<double>.Missing(),
            ObservedValue<double>.Confirmed(0),
            ObservedValue<double>.Missing());

        var rows = LoadoutComparison.Compare(catalog, rock, [Hofstede(), Helix()]);

        Assert.All(rows, row => Assert.Equal(CrackVerdict.Unknown, row.Verdict));
        Assert.All(rows, row => Assert.Equal("", row.Boosts));
    }

    [Fact]
    public void A_small_rock_is_crackable_and_hofstede_keeps_the_wider_window()
    {
        var rows = LoadoutComparison.Compare(Catalog(), RockReading.Confirmed(5000, 0, 40), [Hofstede(), Helix()]);

        var cleanup = rows.Single(row => row.Name == "Cleanup");
        var heavy = rows.Single(row => row.Name == "Heavy");
        Assert.Equal(CrackVerdict.Crackable, cleanup.Verdict);
        Assert.Equal(CrackVerdict.Crackable, heavy.Verdict);
        Assert.Equal("", cleanup.Boosts);
        Assert.Equal("", heavy.Boosts);
        Assert.True(cleanup.ChargeWindow > heavy.ChargeWindow);
    }

    [Fact]
    public void Hofstede_stays_uncrackable_when_helix_needs_surge_and_sabir()
    {
        var rows = LoadoutComparison.Compare(Catalog(), RockReading.Confirmed(20000, 50, 0), [Hofstede(), HelixWithSabir()]);

        var cleanup = rows.Single(row => row.Name == "Cleanup");
        var heavy = rows.Single(row => row.Name == "Heavy");
        Assert.Equal(CrackVerdict.Uncrackable, cleanup.Verdict);
        Assert.Equal("", cleanup.Boosts);
        Assert.Equal(CrackVerdict.Crackable, heavy.Verdict);
        Assert.Equal("Surge Module + Sabir", heavy.Boosts);
    }

    [Fact]
    public void The_known_helix_screen_is_difficult_just_under_a_crack_and_uncrackable_below_that()
    {
        var catalog = Catalog();
        var loadout = new[] { HelixWithSabir() };

        var at31200 = LoadoutComparison.Compare(catalog, RockReading.Confirmed(31200, 50, 0), loadout).Single();
        var at31300 = LoadoutComparison.Compare(catalog, RockReading.Confirmed(31300, 50, 0), loadout).Single();

        Assert.Equal(CrackVerdict.Difficult, at31200.Verdict);
        Assert.Equal(CrackVerdict.Uncrackable, at31300.Verdict);
        Assert.Equal("", at31200.Boosts);
        Assert.Equal("", at31300.Boosts);
    }

    [Fact]
    public void An_unknown_module_does_not_become_crackable()
    {
        var broken = Hofstede();
        broken.Turrets[0].Modules = ["missing-module"];

        var rows = LoadoutComparison.Compare(Catalog(), RockReading.Confirmed(5000, 0, 0), [broken]);

        Assert.Equal(CrackVerdict.Unknown, rows[0].Verdict);
    }

    private static MiningCatalog Catalog() =>
        MiningCatalog.Parse(File.ReadAllText(FindCatalog()));

    private static StoredLoadout Hofstede() =>
        new()
        {
            Name = "Cleanup",
            ShipId = "argo-mole",
            Turrets =
            [
                Laser("hofstede-s2-mining-laser", true, "focus-iii-module", "xtr-xl-module"),
                Laser("arbor-mh2-mining-laser", false),
                Laser("arbor-mh2-mining-laser", false)
            ]
        };

    private static StoredLoadout Helix() =>
        new()
        {
            Name = "Heavy",
            ShipId = "argo-mole",
            Turrets =
            [
                Laser("helix-ii-mining-laser", true, "xtr-xl-module", "surge-module", "rieger-c3-module")
            ]
        };

    private static StoredLoadout HelixWithSabir()
    {
        var loadout = Helix();
        loadout.GadgetId = "sabir";
        return loadout;
    }

    private static StoredTurret Laser(string headId, bool on, params string[] modules) =>
        new()
        {
            HeadId = headId,
            LaserOn = on,
            CraftPercent = "0",
            Modules = modules.Cast<string?>().ToList()
        };

    private static string FindCatalog()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "data", "mining-equipment.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("data/mining-equipment.json was not found above the test output.");
    }
}
