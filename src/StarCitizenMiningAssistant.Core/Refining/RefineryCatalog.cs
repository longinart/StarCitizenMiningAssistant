using System.Text.Json;

namespace StarCitizenMiningAssistant.Core.Refining;

public sealed record RefineryMethod(string Name, double YieldPercent);

public sealed record RefineryStation(string Name, IReadOnlyDictionary<string, double> Bonuses);

public sealed record CommoditySell(string Mineral, string Kind, string Terminal, long Price);

public sealed class RefineryCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private RefineryCatalog(
        string source,
        string retrieved,
        string gameVersion,
        string note,
        IReadOnlyList<RefineryMethod> methods,
        IReadOnlyList<RefineryStation> refineries,
        IReadOnlyList<CommoditySell> sells)
    {
        Source = source;
        Retrieved = retrieved;
        GameVersion = gameVersion;
        Note = note;
        Methods = methods;
        Refineries = refineries;
        Sells = sells;
    }

    public string Source { get; }

    public string Retrieved { get; }

    public string GameVersion { get; }

    public string Note { get; }

    public IReadOnlyList<RefineryMethod> Methods { get; }

    public IReadOnlyList<RefineryStation> Refineries { get; }

    public IReadOnlyList<CommoditySell> Sells { get; }

    public static RefineryCatalog Empty { get; } = new("", "", "", "", [], [], []);

    public static RefineryCatalog Parse(string json)
    {
        var file = JsonSerializer.Deserialize<FileDto>(json, JsonOptions)
            ?? throw new JsonException("Refinery file was empty.");
        var methods = (file.Methods ?? [])
            .Where(method => !string.IsNullOrWhiteSpace(method.Name) && method.YieldPercent is not null)
            .Select(method => new RefineryMethod(method.Name!.Trim(), method.YieldPercent!.Value))
            .ToList();
        var refineries = (file.Refineries ?? [])
            .Where(station => !string.IsNullOrWhiteSpace(station.Name))
            .Select(station => new RefineryStation(
                station.Name!.Trim(),
                (station.Bonuses ?? [])
                    .Where(bonus => !string.IsNullOrWhiteSpace(bonus.Mineral) && bonus.Percent is not null)
                    .GroupBy(bonus => bonus.Mineral!.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.Last().Percent!.Value, StringComparer.OrdinalIgnoreCase)))
            .ToList();
        var sells = (file.Sells ?? [])
            .Where(sell => !string.IsNullOrWhiteSpace(sell.Mineral) && !string.IsNullOrWhiteSpace(sell.Terminal) && sell.Price is > 0)
            .Select(sell => new CommoditySell(sell.Mineral!.Trim(), (sell.Kind ?? "").Trim(), sell.Terminal!.Trim(), sell.Price!.Value))
            .ToList();
        return new RefineryCatalog(
            file.Source ?? "",
            file.Retrieved ?? "",
            file.GameVersion ?? "",
            file.Note ?? "",
            methods,
            refineries,
            sells);
    }

    private sealed class FileDto
    {
        public string? Source { get; set; }

        public string? Retrieved { get; set; }

        public string? GameVersion { get; set; }

        public string? Note { get; set; }

        public List<MethodDto>? Methods { get; set; }

        public List<StationDto>? Refineries { get; set; }

        public List<SellDto>? Sells { get; set; }
    }

    private sealed class MethodDto
    {
        public string? Name { get; set; }

        public double? YieldPercent { get; set; }
    }

    private sealed class StationDto
    {
        public string? Name { get; set; }

        public List<BonusDto>? Bonuses { get; set; }
    }

    private sealed class BonusDto
    {
        public string? Mineral { get; set; }

        public double? Percent { get; set; }
    }

    private sealed class SellDto
    {
        public string? Mineral { get; set; }

        public string? Kind { get; set; }

        public string? Terminal { get; set; }

        public long? Price { get; set; }
    }
}
