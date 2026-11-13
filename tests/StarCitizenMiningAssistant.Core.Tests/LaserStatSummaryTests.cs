using StarCitizenMiningAssistant.Core.Calculation;

namespace StarCitizenMiningAssistant.Core.Tests;

public class LaserStatSummaryTests
{
    [Fact]
    public void Head_without_modules_keeps_its_catalog_modifiers()
    {
        var head = LaserStatParts.None with
        {
            ResistancePercent = -30,
            GreenZonePercent = -40,
            AllChargeRatesPercent = 40,
            InertPercent = -40
        };

        var summary = LaserStatSummary.Combine(1232.5, 4930, 0, 30, 90, head, []);

        Assert.Equal(4930, summary.PowerWatts);
        Assert.Equal(1232.5, summary.MinPowerWatts);
        Assert.Equal(30, summary.OptimalRange);
        Assert.Equal(90, summary.MaximumRange);
        Assert.Equal(-30, summary.ResistancePercent, 5);
        Assert.Equal(0, summary.InstabilityPercent, 5);
        Assert.Equal(0, summary.ChargeRatePercent, 5);
        Assert.Equal(40, summary.AllChargeRatesPercent, 5);
        Assert.Equal(-40, summary.WindowSizePercent, 5);
        Assert.Equal(0, summary.OverchargePercent, 5);
        Assert.Equal(-40, summary.InertPercent, 5);
    }

    [Fact]
    public void Rieger_c3_and_surge_multiply_onto_helix_ii()
    {
        var head = LaserStatParts.None with
        {
            ResistancePercent = -30,
            GreenZonePercent = -40
        };
        var rieger = LaserStatParts.None with { PowerPercent = 25, GreenZonePercent = -1 };
        var surge = LaserStatParts.None with { PowerPercent = 50, ResistancePercent = -16, InstabilityPercent = 10 };

        var summary = LaserStatSummary.Combine(0, 4930, 0, 0, 0, head, [rieger, surge]);

        Assert.Equal(9243.75, summary.PowerWatts);
        Assert.Equal(-41.2, summary.ResistancePercent, 5);
        Assert.Equal(10, summary.InstabilityPercent, 5);
        Assert.Equal(0, summary.ChargeRatePercent);
        Assert.Equal(-40.6, summary.WindowSizePercent, 5);
    }

    [Fact]
    public void Craft_percent_scales_laser_power_and_leaves_extract_power_separate()
    {
        var module = LaserStatParts.None with { PowerPercent = -15, ExtractPowerPercent = 50 };

        var summary = LaserStatSummary.Combine(0, 3900, 10, 0, 0, LaserStatParts.None, [module]);

        Assert.Equal(3646.5, summary.PowerWatts);
        Assert.Equal(50, summary.ExtractPowerPercent, 5);
    }

    [Fact]
    public void Overcharge_shatter_and_inert_use_the_same_product_as_resistance()
    {
        var head = LaserStatParts.None with { InertPercent = -40 };
        var module = LaserStatParts.None with
        {
            OverchargePercent = -80,
            ShatterPercent = 40,
            InertPercent = -6,
            ExtractPowerPercent = -5
        };

        var summary = LaserStatSummary.Combine(0, 1000, 0, 0, 0, head, [module]);

        Assert.Equal(-80, summary.OverchargePercent, 5);
        Assert.Equal(40, summary.ShatterPercent, 5);
        Assert.Equal(-43.6, summary.InertPercent, 5);
        Assert.Equal(-5, summary.ExtractPowerPercent, 5);
    }
}
