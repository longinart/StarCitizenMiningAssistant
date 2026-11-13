using StarCitizenMiningAssistant.Core.Calculation;
using StarCitizenMiningAssistant.Core.Equipment;
using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.Core.Tests;

public class CrackabilityCalculatorTests
{
    [Fact]
    public void Published_default_rock_with_3900w_matches_the_scminer_page()
    {
        var result = CrackabilityCalculator.Assess(
            RockReading.Confirmed(5000, 0, 0),
            [new LaserContribution(3900)]);

        Assert.Equal(1800, result.PowerRequired);
        Assert.Equal(3900, result.PowerDelivered);
        Assert.Equal(10833, result.MaxCrackableMass);
        Assert.Equal(50, result.RecommendedThrottlePercent);
        Assert.True(result.CanCrack);
        Assert.Equal(CrackBand.Trivial, result.Band);
    }

    [Fact]
    public void Arbor_mh1_on_the_published_default_rock_is_a_130_percent_ratio()
    {
        var result = CrackabilityCalculator.Assess(
            RockReading.Confirmed(5000, 0, 0),
            [new LaserContribution(2340)]);

        Assert.Equal(6500, result.MaxCrackableMass);
        Assert.Equal(1.3, result.PowerRatio);
        Assert.Equal(CrackBand.Comfortable, result.Band);
    }

    [Fact]
    public void Pitman_charge_rate_matches_the_page_crack_time()
    {
        var pitman = new LaserContribution(
            3900,
            0,
            new ModifierSet(0, 25, 35, 40, -40, 0),
            []);

        var result = CrackabilityCalculator.Assess(RockReading.Confirmed(5000, 0, 0), [pitman]);

        Assert.Equal(0, result.EffectiveResistance);
        Assert.Equal(23.3, result.EstimatedCrackTimeSeconds);
        Assert.Equal(50, result.RecommendedThrottlePercent);
    }

    [Fact]
    public void Head_resistance_scales_the_rock_before_power_is_reduced()
    {
        var helix = new LaserContribution(3900, 0, new ModifierSet(0, -30, 0, 0, 0, 0), []);

        var result = CrackabilityCalculator.Assess(RockReading.Confirmed(5000, 40, 0), [helix]);

        Assert.Equal(28, result.EffectiveResistance);
        Assert.Equal(2808, result.PowerDelivered);
        Assert.Equal(1800, result.PowerRequired);
    }

    [Fact]
    public void Two_lasers_sum_power_and_average_resistance()
    {
        LaserContribution[] lasers =
        [
            new(3900, 0, new ModifierSet(0, -30, 0, 0, 0, 0), []),
            new(2340, 0, new ModifierSet(0, 25, 0, 0, 0, 0), [])
        ];

        var result = CrackabilityCalculator.Assess(RockReading.Confirmed(5000, 40, 0), lasers);

        Assert.Equal(39, result.EffectiveResistance);
        Assert.Equal(3806.4, result.PowerDelivered!.Value, 1);
    }

    [Fact]
    public void Passive_module_changes_only_its_own_laser()
    {
        var boosted = new ModifierSet(50, -50, 0, 0, 0, 0);
        LaserContribution[] lasers =
        [
            new(3900, 0, ModifierSet.None, [boosted]),
            new(2340)
        ];

        var result = CrackabilityCalculator.Assess(RockReading.Confirmed(5000, 40, 0), lasers);

        Assert.Equal(30, result.EffectiveResistance);
        Assert.Equal(5733, result.PowerDelivered!.Value, 1);
    }

    [Fact]
    public void Sabir_and_surge_multiply_the_fleet_and_stack_together()
    {
        var sabir = new ModifierSet(0, -50, 15, 50, 0, 0);
        var surge = new ModifierSet(50, -16, 10, 0, 0, 0);
        var lasers = new[] { new LaserContribution(3900) };
        var rock = RockReading.Confirmed(5000, 40, 10);

        var gadget = CrackabilityCalculator.Assess(rock, lasers, sabir);
        var consumable = CrackabilityCalculator.Assess(rock, lasers, consumable: surge);
        var both = CrackabilityCalculator.Assess(rock, lasers, sabir, surge);

        Assert.Equal(20, gadget.EffectiveResistance);
        Assert.Equal(3120, gadget.PowerDelivered);
        Assert.Equal(33.6, consumable.EffectiveResistance);
        Assert.Equal(3884.4, consumable.PowerDelivered!.Value, 1);
        Assert.Equal(16.8, both.EffectiveResistance);
        Assert.Equal(4867.2, both.PowerDelivered!.Value, 1);
        Assert.True(both.PowerDelivered > gadget.PowerDelivered);
        Assert.True(both.PowerDelivered > consumable.PowerDelivered);
    }

    [Fact]
    public void Craft_percent_multiplies_that_head_before_modules()
    {
        var result = CrackabilityCalculator.Assess(
            RockReading.Confirmed(5000, 0, 0),
            [new LaserContribution(3900, 10, ModifierSet.None, [])]);

        Assert.Equal(4290, result.PowerDelivered);
        Assert.Equal(11917, result.MaxCrackableMass);
    }

    [Fact]
    public void Missing_instability_does_not_block_the_crack_verdict()
    {
        var rock = new RockReading(
            ObservedValue<double>.Confirmed(5000),
            ObservedValue<double>.Confirmed(0),
            ObservedValue<double>.Missing());

        var result = CrackabilityCalculator.Assess(rock, [new LaserContribution(3900)]);

        Assert.True(result.CanCrack);
        Assert.Null(result.EffectiveInstability);
    }

    [Fact]
    public void Missing_mass_stays_unknown()
    {
        var rock = new RockReading(
            ObservedValue<double>.Missing(),
            ObservedValue<double>.Confirmed(0),
            ObservedValue<double>.Confirmed(0));

        var result = CrackabilityCalculator.Assess(rock, [new LaserContribution(3900)]);

        Assert.Equal(CrackBand.Unknown, result.Band);
        Assert.Null(result.CanCrack);
    }

    [Fact]
    public void Assistance_sentence_distinguishes_gadget_surge_and_both()
    {
        var bare = Assessment(false);
        var cracks = Assessment(true);

        Assert.Equal(
            "Sabir alone is enough.",
            CrackabilityCalculator.DescribeAssistance(bare, cracks, Assessment(false), cracks, "Sabir", "Surge"));
        Assert.Equal(
            "Surge alone is enough.",
            CrackabilityCalculator.DescribeAssistance(bare, Assessment(false), cracks, cracks, "Sabir", "Surge"));
        Assert.Equal(
            "You need Sabir and Surge together.",
            CrackabilityCalculator.DescribeAssistance(bare, Assessment(false), Assessment(false), cracks, "Sabir", "Surge"));
        Assert.Equal(
            "Sabir alone is enough, and Surge alone is enough. You do not need both.",
            CrackabilityCalculator.DescribeAssistance(bare, cracks, cracks, cracks, "Sabir", "Surge"));
    }

    private static CrackAssessment Assessment(bool canCrack) =>
        new(
            CrackabilityCalculator.FormulaVersion,
            canCrack ? CrackBand.Comfortable : CrackBand.NeedsMoreLasers,
            canCrack,
            1,
            canCrack ? 2 : 0,
            canCrack ? 2 : 0,
            0,
            0,
            18,
            canCrack ? 50 : null,
            0,
            14,
            "test");
}

public class MiningCatalogTests
{
    [Fact]
    public void Catalog_matches_the_wiki_records_used_by_the_checker()
    {
        var catalog = MiningCatalog.Parse(File.ReadAllText(FindCatalog()));

        var helix = catalog.FindHead("helix-i-mining-laser");
        var pitman = catalog.FindHead("pitman-mining-laser");
        var klein = catalog.FindHead("klein-s1-mining-laser");
        var surge = catalog.FindModule("surge-module");
        var sabir = catalog.FindGadget("sabir");

        Assert.NotNull(helix);
        Assert.Equal(3900, helix.MaxPower);
        Assert.Equal(-30, helix.Resistance);
        Assert.Equal(0, helix.OptimalChargeRate);
        Assert.Equal(30, helix.AllChargeRates);
        Assert.NotNull(pitman);
        Assert.Equal(-40, pitman.OptimalChargeRate);
        Assert.NotNull(klein);
        Assert.Equal(0, klein.ModuleSlots);
        Assert.NotNull(surge);
        Assert.Equal(50, surge.PowerPercent);
        Assert.Equal(-16, surge.Resistance);
        Assert.Equal(0, surge.ExtractPowerPercent);
        Assert.NotNull(sabir);
        Assert.Equal(-50, sabir.Resistance);
        Assert.Equal(15, sabir.Instability);
        Assert.Equal("4.10.2-LIVE.12881860", catalog.GameVersion);
        Assert.Contains(catalog.Ships, ship => ship.Id == "argo-mole" && ship.Turrets.Count == 3);
        Assert.DoesNotContain(catalog.Heads, head => head.Size == 0);
        Assert.DoesNotContain(catalog.Heads, head => head.Id is "arbor-mhv-mining-laser" or "lawson-mining-laser");

        var agricium = catalog.Materials.Single(material => material.Name == "Agricium");
        var quantainium = catalog.Materials.Single(material => material.Name == "Quantainium");
        Assert.Equal(3885, agricium.Signature);
        Assert.Equal(10, agricium.DepositSignatures.Count);
        Assert.Equal(3170, quantainium.Signature);
        var agriciumDeposit = catalog.MatchSignature(7770);
        Assert.Contains(agriciumDeposit, match => match.Material.Name == "Agricium" && match.DepositNumber == 2);
    }

    [Fact]
    public void One_helix_ii_with_rieger_c3_surge_and_sabir_matches_the_31300kg_screen()
    {
        var catalog = MiningCatalog.Parse(File.ReadAllText(FindCatalog()));
        var helix = catalog.FindHead("helix-ii-mining-laser");
        var rieger = catalog.FindModule("rieger-c3-module");
        var xtr = catalog.FindModule("xtr-xl-module");
        var surge = catalog.FindModule("surge-module");
        var sabir = catalog.FindGadget("sabir");
        Assert.NotNull(helix);
        Assert.NotNull(rieger);
        Assert.NotNull(xtr);
        Assert.NotNull(surge);
        Assert.NotNull(sabir);
        Assert.Equal(0, xtr.PowerPercent);

        var laser = new LaserContribution(
            helix.MaxPower,
            0,
            new ModifierSet(0, helix.Resistance, helix.Instability, helix.GreenZone, helix.OptimalChargeRate, 0),
            [
                new ModifierSet(rieger.PowerPercent, rieger.Resistance, rieger.Instability, rieger.GreenZone, rieger.OptimalChargeRate, rieger.Shatter),
                new ModifierSet(xtr.PowerPercent, xtr.Resistance, xtr.Instability, xtr.GreenZone, xtr.OptimalChargeRate, xtr.Shatter),
                new ModifierSet(surge.PowerPercent, surge.Resistance, surge.Instability, surge.GreenZone, surge.OptimalChargeRate, surge.Shatter)
            ]);
        var gadget = new ModifierSet(sabir.PowerPercent, sabir.Resistance, sabir.Instability, sabir.GreenZone, sabir.OptimalChargeRate, 0);

        var result = CrackabilityCalculator.Assess(RockReading.Confirmed(31300, 50, 0), [laser], gadget);

        Assert.Equal(11268, result.PowerRequired);
        Assert.Equal(7885, result.PowerDelivered!.Value, 0);
        Assert.False(result.CanCrack);
        Assert.Equal(70, (int)Math.Round(result.PowerRatio!.Value * 100, MidpointRounding.AwayFromZero));
    }

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
