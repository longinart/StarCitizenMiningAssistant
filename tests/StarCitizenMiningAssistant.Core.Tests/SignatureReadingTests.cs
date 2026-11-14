using StarCitizenMiningAssistant.Core.Equipment;
using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.Core.Tests;

public class SignatureReadingTests
{
    [Fact]
    public void One_catalog_deposit_confirms_the_signature_and_names_the_deposit()
    {
        var catalog = Catalog();
        var bexalite = SignatureReading.FromLines(catalog, [Line("7200")]);
        var third = SignatureReading.FromLines(catalog, [Line("10,800")]);

        Assert.True(bexalite.IsConfirmed);
        Assert.Equal(7200, bexalite.Value);
        Assert.True(third.IsConfirmed);
        Assert.Equal(10800, third.Value);

        var hit = Assert.Single(SignatureWatch.Evaluate(catalog, bexalite, ["Bexalite"], soundEnabled: true).Hits);
        Assert.Equal("2 × Bexalite", hit.Label);
        Assert.True(hit.Blink);
        Assert.Equal("3 × Bexalite", Assert.Single(SignatureWatch.Evaluate(catalog, third, [], soundEnabled: false).Hits).Label);
    }

    [Fact]
    public void A_near_miss_two_numbers_or_a_blank_line_stays_unconfirmed()
    {
        var catalog = Catalog();
        var near = SignatureReading.FromLines(catalog, [Line("7201")]);
        var both = SignatureReading.FromLines(catalog, [Line("7200"), Line("10800")]);
        var blank = SignatureReading.FromLines(catalog, [Line("signature")]);

        Assert.Equal(RecognitionConfidence.Uncertain, near.Confidence);
        Assert.Equal(7201, near.Value);
        Assert.Equal(RecognitionConfidence.Uncertain, both.Confidence);
        Assert.Equal(RecognitionConfidence.Missing, blank.Confidence);
        Assert.Empty(SignatureWatch.Evaluate(catalog, near, ["Bexalite"], soundEnabled: true).Hits);
    }

    [Fact]
    public void A_number_shared_by_two_minerals_stays_uncertain()
    {
        var catalog = MiningCatalog.Parse("""
            {
              "heads": [ { "id": "head" } ],
              "materials": [
                { "name": "Alpha", "signature": 100, "depositSignatures": [100, 200] },
                { "name": "Beta", "signature": 100, "depositSignatures": [100] }
              ]
            }
            """);

        var shared = SignatureReading.FromLines(catalog, [Line("100")]);
        var only = SignatureReading.FromLines(catalog, [Line("200")]);

        Assert.Equal(RecognitionConfidence.Uncertain, shared.Confidence);
        Assert.True(only.IsConfirmed);
        Assert.Equal("2 × Alpha", Assert.Single(SignatureWatch.Evaluate(catalog, only, ["Alpha"], soundEnabled: false).Hits).Label);
        Assert.Empty(SignatureWatch.Evaluate(catalog, shared, ["Alpha", "Beta"], soundEnabled: true).Hits);
    }

    private static ObservedValue<string> Line(string text) => ObservedValue<string>.Uncertain(text);

    private static MiningCatalog Catalog() => MiningCatalog.Parse(File.ReadAllText(FindCatalog()));

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
