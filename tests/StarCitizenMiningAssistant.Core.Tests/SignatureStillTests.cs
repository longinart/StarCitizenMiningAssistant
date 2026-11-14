using StarCitizenMiningAssistant.Core.Equipment;
using StarCitizenMiningAssistant.Infrastructure.Capture;

namespace StarCitizenMiningAssistant.Core.Tests;

public class SignatureStillTests
{
    public static IEnumerable<object[]> Stills()
    {
        // The bool is true when this frame must produce the number. A false frame
        // may come back empty, and it must not come back as a different number.
        yield return ["signatures-20261113-061435.bmp", "10800", true];
        yield return ["signatures-20261113-061437.bmp", "10800", true];
        yield return ["signatures-20261113-061444.bmp", "10800", true];
        yield return ["signatures-20261113-061459.bmp", "2000", false];
        yield return ["signatures-20261113-063041.bmp", "", true];
        yield return ["signatures-20261113-063947.bmp", "7200", true];
        yield return ["signatures-20261113-064003.bmp", "", true];
        yield return ["signatures-20261113-064009.bmp", "7200", true];
        yield return ["signatures-20261113-064016.bmp", "7200", true];
        yield return ["signatures-20261113-065310.bmp", "", true];
        yield return ["signatures-20261113-065312.bmp", "", true];
        yield return ["signatures-20261113-065456.bmp", "", true];
        yield return ["signatures-20261113-065501.bmp", "", true];
        yield return ["signatures-20261113-065514.bmp", "8250", true];
        yield return ["signatures-20261113-090029.bmp", "8250", true];
        yield return ["signatures-20261113-154322.bmp", "", true];
        yield return ["signatures-20261113-154341.bmp", "8250", true];
        yield return ["signatures-20261113-154357.bmp", "8250", true];
        yield return ["signatures-20261113-154411.bmp", "8250", true];
        yield return ["signatures-20261114-070753.bmp", "3400", true];
        yield return ["signatures-20261114-070825.bmp", "3400", true];
        yield return ["signatures-20261114-070840.bmp", "3400", true];
        yield return ["signatures-20261114-071142.bmp", "6000", true];
        yield return ["signatures-20261114-071145.bmp", "6000", true];
        yield return ["signatures-20261114-071148.bmp", "6000", true];
        yield return ["signatures-20261114-071149.bmp", "6000", true];
        yield return ["signatures-20261114-071150.bmp", "6000", true];
        yield return ["signatures-20261114-071151.bmp", "6000", true];
        yield return ["signatures-20261114-071152.bmp", "6000", true];
        yield return ["signatures-20261114-071153.bmp", "6000", false];
        yield return ["signatures-20261114-071209.bmp", "6000", false];
        yield return ["signatures-20261114-071230.bmp", "2000", true];
        yield return ["signatures-20261114-071243.bmp", "6800", true];
        yield return ["signatures-20261114-071325.bmp", "6800", false];
        yield return ["signatures-20261114-071827.bmp", "6800", false];
    }

    [Theory]
    [MemberData(nameof(Stills))]
    public async Task A_saved_signature_reads_the_visible_number(string file, string expected, bool required)
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Signatures");
        var image = await File.ReadAllBytesAsync(Path.Combine(folder, file));
        var document = await SignatureOcr.ReadAsync(image, Catalog());
        var actual = document.Lines.Count == 0 ? "" : document.Lines[0].Value ?? "";
        var acceptable = actual == expected || (!required && actual.Length == 0);
        Assert.True(acceptable, file + " read '" + actual + "'");
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
