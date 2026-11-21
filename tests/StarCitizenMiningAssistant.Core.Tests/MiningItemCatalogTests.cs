using StarCitizenMiningAssistant.Core.Equipment;

namespace StarCitizenMiningAssistant.Core.Tests;

public class MiningItemCatalogTests
{
    [Fact]
    public void Search_matches_name_or_category_and_stays_alphabetical()
    {
        var catalog = MiningItemCatalog.Parse("""
            {
              "source": "https://api.uexcorp.uk/2.0/",
              "retrieved": "2026-11-20",
              "gameVersion": "4.10.3",
              "items": [
                { "id": "surge", "name": "Surge Module", "category": "Mining module", "description": "Active module.", "purchaseLocations": [ { "verse": "Nyx, Levski", "price": 1400, "type": "buy", "gameVersion": "4.10.2" } ] },
                { "id": "sabir", "name": "Sabir", "category": "Mining gadget", "description": "Gadget.", "purchaseLocations": [] }
              ],
              "vehicles": [
                { "id": "mole", "name": "MOLE", "category": "Ship", "oreCapacity": 96, "crewMin": 4, "crewMax": 2, "rentCost": 223256, "purchaseLocations": [] },
                { "id": "prospector", "name": "MISC Prospector", "category": "Ship", "oreCapacity": 32, "crewMin": 1, "crewMax": 1, "purchaseLocations": [] }
              ]
            }
            """);

        Assert.Equal(["Sabir", "Surge Module"], catalog.Search("").Select(item => item.Name).ToArray());
        Assert.Equal("Surge Module", catalog.Search("surge").Single().Name);
        Assert.Equal("Sabir", catalog.Search("gadget").Single().Name);
        Assert.Empty(catalog.Search("zzz"));
        Assert.Equal(["MISC Prospector", "MOLE"], catalog.Vehicles.Select(vehicle => vehicle.Name).ToArray());
        Assert.Null(catalog.Search("sabir").Single().PurchaseLocations.SingleOrDefault()?.Price);
        Assert.Equal(1400, catalog.Search("surge").Single().PurchaseLocations[0].Price);
    }

    [Fact]
    public void The_shipped_file_loads_sourced_rows()
    {
        var catalog = MiningItemCatalog.Parse(File.ReadAllText(FindFile()));

        Assert.Equal("4.10.3", catalog.GameVersion);
        Assert.Contains(catalog.Items, item => item.Name == "Surge Module");
        var sabir = catalog.Items.Single(item => item.Name == "Sabir");
        Assert.Equal(34, sabir.PurchaseLocations.Count);
        Assert.Contains(sabir.PurchaseLocations, offer => offer.Verse.Contains("Tammany And Sons", StringComparison.Ordinal) && offer.Price == 13125);
        Assert.Equal(13125, sabir.PurchaseLocations.Min(offer => offer.Price));
        Assert.Contains(catalog.Vehicles, vehicle => vehicle.Name == "MISC Prospector" && vehicle.OreCapacity == 32);
        Assert.Contains(catalog.Vehicles, vehicle => vehicle.Name == "Greycat ROC" && vehicle.OreCapacity is null);
        Assert.All(catalog.Items, item => Assert.DoesNotContain("http", item.Description, StringComparison.OrdinalIgnoreCase));
    }

    private static string FindFile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "data", "mining-items.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("data/mining-items.json was not found above the test output.");
    }
}
