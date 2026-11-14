using StarCitizenMiningAssistant.Core.Equipment;
using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.Core.Tests;

public class ScanLogPolicyTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 14, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_changed_reading_is_kept_and_a_repeat_waits()
    {
        Assert.True(ScanLogPolicy.ShouldSave(null, null, "3400", Start));
        Assert.False(ScanLogPolicy.ShouldSave("3400", Start, "3400", Start.AddSeconds(3)));
        Assert.True(ScanLogPolicy.ShouldSave("3400", Start, "3400", Start.AddSeconds(4)));
        Assert.True(ScanLogPolicy.ShouldSave("3400", Start, "6800", Start.AddSeconds(1)));
        Assert.False(ScanLogPolicy.ShouldSave("", Start, "", Start.AddSeconds(9)));
        Assert.True(ScanLogPolicy.ShouldSave("", Start, "", Start.AddSeconds(10)));
        Assert.True(ScanLogPolicy.ShouldSave("", Start, "3400", Start.AddSeconds(1)));
    }

    [Fact]
    public void A_signature_log_line_names_one_catalog_deposit()
    {
        var catalog = Catalog();
        var lines = new[] { ObservedValue<string>.Uncertain("3400") };
        var record = ScanLogText.Signature(catalog, lines, clipped: false);

        Assert.Equal("3400", record.Reading);
        Assert.Equal("Confirmed", record.Confidence);
        Assert.Contains("Lindinium", record.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void An_empty_signature_frame_stays_missing()
    {
        var record = ScanLogText.Signature(Catalog(), null, clipped: false);

        Assert.Equal("", record.Reading);
        Assert.Equal("Missing", record.Confidence);
    }

    private static MiningCatalog Catalog()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "data", "mining-equipment.json");
            if (File.Exists(candidate))
            {
                return MiningCatalog.Parse(File.ReadAllText(candidate));
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("data/mining-equipment.json was not found above the test output.");
    }
}
