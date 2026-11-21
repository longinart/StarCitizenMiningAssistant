using System.Text.Json;

namespace StarCitizenMiningAssistant.Core.Equipment;

public sealed record PurchaseOffer(string Verse, long? Price, string Type, string GameVersion);

public sealed record MiningItem(
    string Id,
    string Name,
    string Category,
    string Description,
    IReadOnlyList<PurchaseOffer> PurchaseLocations);

public sealed record MiningVehicle(
    string Id,
    string Name,
    string Category,
    double? OreCapacity,
    int CrewMin,
    int CrewMax,
    long? RentCost,
    IReadOnlyList<PurchaseOffer> PurchaseLocations);

public sealed class MiningItemCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private MiningItemCatalog(
        string source,
        string retrieved,
        string gameVersion,
        string note,
        IReadOnlyList<MiningItem> items,
        IReadOnlyList<MiningVehicle> vehicles)
    {
        Source = source;
        Retrieved = retrieved;
        GameVersion = gameVersion;
        Note = note;
        Items = items;
        Vehicles = vehicles;
    }

    public string Source { get; }

    public string Retrieved { get; }

    public string GameVersion { get; }

    public string Note { get; }

    public IReadOnlyList<MiningItem> Items { get; }

    public IReadOnlyList<MiningVehicle> Vehicles { get; }

    public static MiningItemCatalog Empty { get; } = new("", "", "", "", [], []);

    public static MiningItemCatalog Parse(string json)
    {
        var file = JsonSerializer.Deserialize<FileDto>(json, JsonOptions)
            ?? throw new JsonException("Mining item file was empty.");
        var items = (file.Items ?? [])
            .Select(MapItem)
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var vehicles = (file.Vehicles ?? [])
            .Select(MapVehicle)
            .OrderBy(vehicle => vehicle.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new MiningItemCatalog(
            file.Source ?? "",
            file.Retrieved ?? "",
            file.GameVersion ?? "",
            file.Note ?? "",
            items,
            vehicles);
    }

    public IReadOnlyList<MiningItem> Search(string? text)
    {
        var query = text?.Trim() ?? "";
        IEnumerable<MiningItem> rows = Items;
        if (query.Length > 0)
        {
            rows = rows.Where(item =>
                item.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.Category.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        return rows.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static MiningItem MapItem(ItemDto item) =>
        new(
            item.Id ?? "",
            item.Name ?? "",
            item.Category ?? "",
            item.Description ?? "",
            MapOffers(item.PurchaseLocations));

    private static MiningVehicle MapVehicle(VehicleDto vehicle) =>
        new(
            vehicle.Id ?? "",
            vehicle.Name ?? "",
            vehicle.Category ?? "",
            vehicle.OreCapacity,
            vehicle.CrewMin,
            vehicle.CrewMax,
            vehicle.RentCost,
            MapOffers(vehicle.PurchaseLocations));

    private static IReadOnlyList<PurchaseOffer> MapOffers(List<OfferDto>? offers) =>
        (offers ?? [])
            .Select(offer => new PurchaseOffer(offer.Verse ?? "", offer.Price, offer.Type ?? "", offer.GameVersion ?? ""))
            .ToList();

    private sealed class FileDto
    {
        public string? Source { get; set; }

        public string? Retrieved { get; set; }

        public string? GameVersion { get; set; }

        public string? Note { get; set; }

        public List<ItemDto>? Items { get; set; }

        public List<VehicleDto>? Vehicles { get; set; }
    }

    private sealed class ItemDto
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? Category { get; set; }

        public string? Description { get; set; }

        public List<OfferDto>? PurchaseLocations { get; set; }
    }

    private sealed class VehicleDto
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? Category { get; set; }

        public double? OreCapacity { get; set; }

        public int CrewMin { get; set; }

        public int CrewMax { get; set; }

        public long? RentCost { get; set; }

        public List<OfferDto>? PurchaseLocations { get; set; }
    }

    private sealed class OfferDto
    {
        public string? Verse { get; set; }

        public long? Price { get; set; }

        public string? Type { get; set; }

        public string? GameVersion { get; set; }
    }
}
