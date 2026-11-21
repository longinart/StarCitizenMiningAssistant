using StarCitizenMiningAssistant.Core.Refining;

namespace StarCitizenMiningAssistant.Core.Tests;

public class RefineryCalculatorTests
{
    [Fact]
    public void Dinyx_on_32_scu_with_no_station_bonus_is_14_4()
    {
        var catalog = RefineryCatalog.Parse("""
            {
              "methods": [ { "name": "Dinyx Solventation", "yieldPercent": 45 } ],
              "refineries": [ { "name": "ARC-L1", "bonuses": [ { "mineral": "Quartz", "percent": 11 } ] } ],
              "sells": [
                { "mineral": "Quantainium", "kind": "refined", "terminal": "Area 18", "price": 200000 },
                { "mineral": "Quantainium", "kind": "refined", "terminal": "Lorville", "price": 180000 },
                { "mineral": "Quantainium", "kind": "raw", "terminal": "Levski", "price": 25000 }
              ]
            }
            """);

        var rock = RefineryCalculator.Calculate(
            32,
            "Dinyx Solventation",
            "ARC-L1",
            [new RefineryRow("Quantainium", 100, true), new RefineryRow("Quartz", 10, false)],
            catalog);

        var quant = Assert.Single(rock.Components);
        Assert.Equal(32, quant.RawScu);
        Assert.Equal(14.4, quant.RefinedScu);
        var prices = CommodityPricing.Quote(rock, catalog);
        Assert.Equal(800000, prices.RawAuec);
        Assert.Equal(2_880_000, prices.RefinedAuec);
        Assert.Equal("Area 18", prices.TopRefined[0].Terminal);
        Assert.Equal("Lorville", prices.TopRefined[1].Terminal);
    }

    [Fact]
    public void Separate_minerals_are_priced_at_their_own_best_shop_and_then_added()
    {
        var catalog = RefineryCatalog.Parse("""
            {
              "methods": [ { "name": "Dinyx Solventation", "yieldPercent": 100 } ],
              "sells": [
                { "mineral": "Quantainium", "kind": "refined", "terminal": "Area 18", "price": 100 },
                { "mineral": "Quantainium", "kind": "refined", "terminal": "Lorville", "price": 10 },
                { "mineral": "Riccite", "kind": "refined", "terminal": "Lorville", "price": 40 }
              ]
            }
            """);

        var rock = RefineryCalculator.Calculate(
            10,
            "Dinyx Solventation",
            "",
            [new RefineryRow("Quantainium", 100, true), new RefineryRow("Riccite", 100, true)],
            catalog);

        Assert.Equal(1400, CommodityPricing.Quote(rock, catalog).RefinedAuec);
    }

    [Fact]
    public void Percentages_over_100_do_not_fit()
    {
        Assert.True(CompositionShares.Fits([35, 65]));
        Assert.True(CompositionShares.Fits([100]));
        Assert.False(CompositionShares.Fits([60, 50]));
        Assert.True(CompositionShares.IsInert("INERT MATERIALS"));
        Assert.True(CompositionShares.IsInert("Inert"));
        Assert.False(CompositionShares.IsInert("Copper"));
        Assert.Equal("Quantainium", CompositionShares.DisplayName("QUANTAINIUM (ORE)", ["Quantainium", "Copper"]));
        Assert.Equal("Tin", CompositionShares.DisplayName("TIN (ORE)", ["Tin", "Copper"]));
        Assert.Equal("Ice", CompositionShares.DisplayName("RAW ICE", ["Ice", "Tin"]));
        Assert.Equal("Ouratite", CompositionShares.DisplayName("Outarite", ["Ouratite"]));
        var names = new[]
        {
            "Beryl", "Bexalite", "Hadanite", "Hephaestanite", "Ice", "Laranite",
            "Quantainium", "Silicon", "Taranite", "Tin", "Titanium"
        };
        Assert.Equal("Hephaestanite", CompositionShares.DisplayName("Haestamte", names));
        Assert.Equal("Hephaestanite", CompositionShares.DisplayName("THaestanite", names));
        Assert.Equal("Hephaestanite", CompositionShares.DisplayName("iPHAESTANITE (R)", names));
        Assert.Equal("Hephaestanite", CompositionShares.DisplayName("Hepha", names));
        Assert.Equal("Silicon", CompositionShares.DisplayName("SILICON sle", names));
        Assert.Equal("Quantainium", CompositionShares.DisplayName("Quantanium", names));
        Assert.Equal("Taranite", CompositionShares.DisplayName("TARANTE", names));
        Assert.Equal("Tin", CompositionShares.DisplayName("TIN", names));
        Assert.Equal("Titanium", CompositionShares.DisplayName("TITAN", names));
        Assert.Null(CompositionShares.DisplayName("MATERIALS e", names));
        Assert.Null(CompositionShares.DisplayName("IANTAJNILJM", names));
    }

    [Fact]
    public void A_station_penalty_scales_the_method_yield()
    {
        var catalog = RefineryCatalog.Parse("""
            {
              "methods": [ { "name": "Dinyx Solventation", "yieldPercent": 45 } ],
              "refineries": [ { "name": "Nyx Gateway", "bonuses": [ { "mineral": "Iron", "percent": -5 }, { "mineral": "Aluminum", "percent": 7 } ] } ]
            }
            """);

        var rock = RefineryCalculator.Calculate(
            32,
            "Dinyx Solventation",
            "Nyx Gateway",
            [new RefineryRow("Iron", 100, true), new RefineryRow("Aluminium", 50, true)],
            catalog);

        Assert.Equal(13.68, rock.Components[0].RefinedScu);
        Assert.Equal(7.704, rock.Components[1].RefinedScu);
    }

    [Fact]
    public void The_shipped_file_has_the_scminer_dinyx_yield()
    {
        var catalog = RefineryCatalog.Parse(File.ReadAllText(FindFile()));

        Assert.Equal("4.10.3", catalog.GameVersion);
        Assert.Equal(45, catalog.Methods.Single(method => method.Name == "Dinyx Solventation").YieldPercent);
        Assert.Contains(catalog.Sells, sell => sell.Mineral == "Bexalite" && sell.Kind == "refined" && sell.Price > 0);
        foreach (var mineral in new[] { "Lindinium", "Ouratite", "Aslarite", "Stileron", "Tin", "Torite" })
        {
            Assert.Contains(catalog.Sells, sell => sell.Mineral == mineral && sell.Kind == "refined" && sell.Price > 0);
        }
    }

    private static string FindFile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "data", "refinery-data.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("data/refinery-data.json was not found above the test output.");
    }
}
