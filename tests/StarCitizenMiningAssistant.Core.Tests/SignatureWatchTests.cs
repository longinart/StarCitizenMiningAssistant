using StarCitizenMiningAssistant.Core.Equipment;
using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.Core.Tests;

public class SignatureWatchTests
{
    [Fact]
    public void A_confirmed_stileron_signature_names_the_mineral_and_blinks_only_when_ticked()
    {
        var catalog = Catalog();
        var reading = ObservedValue<double>.Confirmed(3185);

        var watched = SignatureWatch.Evaluate(catalog, reading, ["Stileron"], soundEnabled: true);
        var hit = Assert.Single(watched.Hits);
        Assert.Equal("Stileron", hit.Mineral);
        Assert.Equal(1, hit.DepositNumber);
        Assert.True(hit.Blink);
        Assert.True(watched.PlaySound);

        var quiet = SignatureWatch.Evaluate(catalog, reading, [], soundEnabled: true);
        Assert.Equal("Stileron", Assert.Single(quiet.Hits).Mineral);
        Assert.False(Assert.Single(quiet.Hits).Blink);
        Assert.False(quiet.PlaySound);
    }

    [Fact]
    public void An_uncertain_signature_does_not_match_or_play_a_sound()
    {
        var result = SignatureWatch.Evaluate(
            Catalog(),
            ObservedValue<double>.Uncertain(3185),
            ["Stileron"],
            soundEnabled: true);

        Assert.Empty(result.Hits);
        Assert.False(result.PlaySound);
    }

    [Fact]
    public void A_ticked_agricium_deposit_blinks_without_sound_when_sound_is_off()
    {
        var result = SignatureWatch.Evaluate(
            Catalog(),
            ObservedValue<double>.Confirmed(7770),
            ["Agricium"],
            soundEnabled: false);

        var hit = Assert.Single(result.Hits);
        Assert.Equal("Agricium", hit.Mineral);
        Assert.Equal(2, hit.DepositNumber);
        Assert.True(hit.Blink);
        Assert.False(result.PlaySound);
    }

    [Fact]
    public void Multiples_of_2000_and_3000_name_salvage_and_hand_deposits_when_ticked()
    {
        var catalog = Catalog();
        var salvage = SignatureWatch.Evaluate(catalog, ObservedValue<double>.Confirmed(10000), [SignatureGroups.Salvage], soundEnabled: true);
        var salvageHit = Assert.Single(salvage.Hits);
        Assert.Equal("5 × Salvage", salvageHit.Label);
        Assert.True(salvageHit.Blink);
        Assert.True(salvage.PlaySound);

        var hand = SignatureWatch.Evaluate(catalog, ObservedValue<double>.Confirmed(81000), [SignatureGroups.HandAndRoc], soundEnabled: false);
        var handHit = Assert.Single(hand.Hits);
        Assert.Equal("27 × Hand & ROC", handHit.Label);
        Assert.True(handHit.Blink);
        Assert.False(hand.PlaySound);

        var chart = Chart();
        var tracked = new[] { SignatureGroups.Salvage, SignatureGroups.HandAndRoc, "Bexalite", "Savrilium", "Lindinium" };
        Assert.Equal("5 × Bexalite", Assert.Single(SignatureWatch.Evaluate(catalog, ObservedValue<double>.Confirmed(18000), tracked, false, chart).Hits).Label);
        Assert.Equal("5 × Savrilium", Assert.Single(SignatureWatch.Evaluate(catalog, ObservedValue<double>.Confirmed(16000), tracked, false, chart).Hits).Label);
        Assert.Equal("5 × Lindinium", Assert.Single(SignatureWatch.Evaluate(catalog, ObservedValue<double>.Confirmed(17000), tracked, false, chart).Hits).Label);
        Assert.Empty(SignatureWatch.Evaluate(catalog, ObservedValue<double>.Confirmed(6000), tracked, false, chart).Hits);
        Assert.Empty(SignatureWatch.Evaluate(catalog, ObservedValue<double>.Confirmed(81000), [], soundEnabled: true).Hits);
    }

    [Fact]
    public void An_unknown_confirmed_number_has_no_mineral()
    {
        var result = SignatureWatch.Evaluate(
            Catalog(),
            ObservedValue<double>.Confirmed(1),
            ["Stileron"],
            soundEnabled: true);

        Assert.Empty(result.Hits);
        Assert.False(result.PlaySound);
    }

    private static MiningCatalog Catalog() => MiningCatalog.Parse(File.ReadAllText(FindData("mining-equipment.json")));

    private static ScanSignatureChart Chart() => ScanSignatureChart.Parse(File.ReadAllText(FindData("scan-signatures.json")));

    private static string FindData(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "data", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("data/" + fileName + " was not found above the test output.");
    }
}
