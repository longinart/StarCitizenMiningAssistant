using StarCitizenMiningAssistant.Core.Navigation;

namespace StarCitizenMiningAssistant.Core.Tests;

public class AaronHaloChartTests
{
    [Fact]
    public void Band_5_routes_from_ARC_L1_match_BandHopper_and_the_widest_margin_is_first()
    {
        var chart = AaronHaloChart.Parse(File.ReadAllText(FindData()));

        Assert.Equal(20, chart.Homes.Count);
        var routes = chart.From("arc-l1");
        Assert.Equal(5, routes.Count);
        Assert.Equal("CRU-L3", routes[0].DestinationShort);
        Assert.Equal("Exit quantum at 12.7 Gm", routes[0].Exit);
        Assert.Equal("Margin: 369 Mm", routes[0].Margin);
        Assert.Equal("Target via HUD - Not selectable on map", routes[0].Note);
        Assert.Equal("ARC-L1", routes[0].ReverseName);
        Assert.Equal("Exit quantum at 10.0 Gm", routes[0].ReverseExit);
        Assert.Equal("Margin: 369 Mm", routes[0].ReverseMargin);
        Assert.Null(routes[0].ReverseNote);
        Assert.True(routes[0].HasReverse);
        Assert.True(routes[0].Recommended);
        Assert.All(chart.Homes, home => Assert.All(chart.From(home.Id), route => Assert.True(route.HasReverse)));
        Assert.Equal("CRU-L4", routes[1].DestinationShort);
        Assert.Equal("Exit quantum at 14.3 Gm", routes[1].Exit);
        Assert.Equal("Margin: 345 Mm", routes[1].Margin);
        Assert.False(routes[1].HasNote);
        Assert.False(routes[1].Recommended);
        Assert.Equal("Hurston", routes[2].DestinationShort);
        Assert.Equal("Exit quantum at 13.5 Gm", routes[2].Exit);
        Assert.Equal("Margin: 192 Mm", routes[2].Margin);
        Assert.Empty(chart.From("not-a-place"));
    }

    private static string FindData()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "data", "aaron-halo-band5.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("data/aaron-halo-band5.json was not found above the test output.");
    }
}
