using StarCitizenMiningAssistant.Core.Equipment;
using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.Core.Tests;

public class SignatureDigitsTests
{
    [Theory]
    [InlineData("10,800", "10800")]
    [InlineData("9 10.800", "10800")]
    [InlineData("8 250", "8250")]
    [InlineData("2.000", "2000")]
    [InlineData("q 34.00", "3400")]
    [InlineData("6", null)]
    [InlineData("e.ooo", null)]
    public void One_ocr_line_keeps_a_whole_signature_number(string text, string? expected)
    {
        Assert.Equal(expected, SignatureDigits.Parse(text));
    }

    [Fact]
    public void Two_readings_keep_the_number_they_share()
    {
        var catalog = Catalog();
        Assert.Equal("10800", SignatureDigits.Choose("10,800", "10 800", catalog));
        Assert.Equal("3400", SignatureDigits.Choose("3,400", null, catalog));
        Assert.Equal("6000", SignatureDigits.Choose("6,000", "6,007", catalog));
        Assert.Equal("6800", SignatureDigits.Choose("0800", "6,800", catalog));
        Assert.Null(SignatureDigits.Choose("2,000", "7,200", catalog));
        Assert.Null(SignatureDigits.Choose("e.ooo", "2", catalog));
        Assert.Null(SignatureDigits.Choose(null, null, catalog));
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
