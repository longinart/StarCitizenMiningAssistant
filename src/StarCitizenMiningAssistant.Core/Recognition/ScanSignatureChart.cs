using System.Text.Json;

namespace StarCitizenMiningAssistant.Core.Recognition;

public readonly record struct SignatureDeposit(string Kind, int Count);

/// <summary>
/// Deposit bases used to decide whether a digit row is a real signature.
/// A cluster signature is count times the base. Decision 021.
/// </summary>
public sealed class ScanSignatureChart
{
    private readonly List<Entry> _entries;

    private ScanSignatureChart(List<Entry> entries) => _entries = entries;

    public bool IsKnown(int value) => Matches(value).Count > 0;

    public IReadOnlyList<SignatureDeposit> Matches(int value)
    {
        if (value <= 0)
        {
            return [];
        }

        return _entries
            .Where(entry => value % entry.Base == 0 && value / entry.Base is var count && count >= 1 && count <= entry.MaxCount)
            .Select(entry => new SignatureDeposit(entry.Kind, value / entry.Base))
            .OrderBy(match => match.Count)
            .ThenBy(match => match.Kind, StringComparer.Ordinal)
            .ToList();
    }

    public static ScanSignatureChart Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var file = JsonSerializer.Deserialize<File>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new FormatException("The signature chart was empty.");
        if (file.Entries is null || file.Entries.Count == 0)
        {
            throw new FormatException("The signature chart has no deposits.");
        }

        var entries = file.Entries.Select(entry =>
        {
            if (string.IsNullOrWhiteSpace(entry.Kind) || entry.Base <= 0 || entry.MaxCount <= 0)
            {
                throw new FormatException("A signature chart entry is incomplete.");
            }

            return new Entry(entry.Kind, entry.Base, entry.MaxCount);
        }).ToList();
        return new ScanSignatureChart(entries);
    }

    private sealed record Entry(string Kind, int Base, int MaxCount);

    private sealed class File
    {
        public List<Row>? Entries { get; set; }
    }

    private sealed class Row
    {
        public string? Kind { get; set; }

        public int Base { get; set; }

        public int MaxCount { get; set; }
    }
}
