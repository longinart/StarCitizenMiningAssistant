using System.Globalization;
using StarCitizenMiningAssistant.Core.Equipment;

namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// Decides which automatic scan frames are worth keeping. A changed reading is
/// kept at once. The same reading is kept only after a gap, and an empty frame
/// waits longer so a dark stretch does not fill the log.
/// </summary>
public static class ScanLogPolicy
{
    public static readonly TimeSpan SameReading = TimeSpan.FromSeconds(4);

    public static readonly TimeSpan EmptyReading = TimeSpan.FromSeconds(10);

    public static bool ShouldSave(string? previous, DateTimeOffset? previousAt, string reading, DateTimeOffset now)
    {
        if (previousAt is null)
        {
            return true;
        }

        if (!string.Equals(previous ?? "", reading ?? "", StringComparison.Ordinal))
        {
            return true;
        }

        var gap = string.IsNullOrEmpty(reading) ? EmptyReading : SameReading;
        return now - previousAt.Value >= gap;
    }
}

public readonly record struct ScanLogRecord(string Reading, string Confidence, string Detail);

/// <summary>
/// Turns one captured frame into the columns of the scan log. The reading is
/// what this frame said. It is not the number the overlay is still holding.
/// </summary>
public static class ScanLogText
{
    public static ScanLogRecord Signature(
        MiningCatalog? catalog,
        IReadOnlyList<ObservedValue<string>>? lines,
        bool clipped,
        ScanSignatureChart? chart = null)
    {
        var reading = Join(lines);
        if (clipped)
        {
            return new ScanLogRecord(reading, "Clipped", "The rectangle clips the digits.");
        }

        if (lines is null || lines.Count == 0 || catalog is null)
        {
            return new ScanLogRecord(reading, RecognitionConfidence.Missing.ToString(), "");
        }

        var observed = SignatureReading.FromLines(catalog, lines, chart);
        var detail = "";
        if (observed.Confidence != RecognitionConfidence.Missing)
        {
            var decoded = chart?.Matches((int)Math.Round(observed.Value)) ?? [];
            if (decoded.Count > 0)
            {
                detail = string.Join("; ", decoded.Select(match => match.Count + " x " + match.Kind));
            }
            else
            {
                var matches = catalog.MatchSignature(observed.Value);
                detail = matches.Count == 0
                    ? "no catalog deposit"
                    : string.Join("; ", matches.Select(match => match.DepositNumber + " x " + match.Material.Name));
            }
        }

        return new ScanLogRecord(reading, observed.Confidence.ToString(), detail);
    }

    public static ScanLogRecord Rock(IReadOnlyList<ObservedValue<string>>? lines)
    {
        if (lines is null || lines.Count == 0)
        {
            return new ScanLogRecord("", RecognitionConfidence.Missing.ToString(), "");
        }

        var fields = RockOcrFields.Read(lines, CultureInfo.InvariantCulture);
        var reading = fields.HasPanel
            ? "mass=" + Part(fields.Mass) + " resistance=" + Part(fields.Resistance) + " instability=" + Part(fields.Instability)
            : "";
        var confidence = fields.Mass.Confidence + ";" + fields.Resistance.Confidence + ";" + fields.Instability.Confidence;
        return new ScanLogRecord(reading, confidence, "");
    }

    private static string Join(IReadOnlyList<ObservedValue<string>>? lines) =>
        lines is null
            ? ""
            : string.Join(" | ", lines.Select(line => line.Value).Where(text => !string.IsNullOrWhiteSpace(text)));

    private static string Part(ObservedValue<double> value) =>
        value.Confidence == RecognitionConfidence.Missing
            ? "missing"
            : value.Value.ToString("0.##", CultureInfo.InvariantCulture) + "/" + value.Confidence;
}
