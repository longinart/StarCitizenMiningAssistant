using System.Collections.ObjectModel;
using System.Globalization;
using StarCitizenMiningAssistant.Core.Equipment;

namespace StarCitizenMiningAssistant.App.ViewModels;

public sealed class MiningEquipmentViewModel : ViewModelBase
{
    private readonly MiningItemCatalog _catalog;
    private string _searchText = "";
    private MiningItemRow? _selectedItem;
    private MiningVehicleRow? _selectedVehicle;

    public MiningEquipmentViewModel(MiningItemCatalog catalog)
    {
        _catalog = catalog ?? MiningItemCatalog.Empty;
        Note = string.IsNullOrWhiteSpace(_catalog.Note)
            ? "No mining item file was loaded."
            : _catalog.Note;
        Vehicles = new ObservableCollection<MiningVehicleRow>(
            _catalog.Vehicles.Select(MiningVehicleRow.From));
        FilteredItems = [];
        ApplySearch();
    }

    public static MiningEquipmentViewModel Empty { get; } = new(MiningItemCatalog.Empty);

    public string Note { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (Set(ref _searchText, value))
            {
                CloseItem();
                OnPropertyChanged(nameof(ShowSearchPrompt));
                ApplySearch();
            }
        }
    }

    public bool ShowSearchPrompt => string.IsNullOrWhiteSpace(SearchText);

    public ObservableCollection<MiningItemRow> FilteredItems { get; }

    public MiningItemRow? SelectedItem
    {
        get => _selectedItem;
        private set
        {
            if (Set(ref _selectedItem, value))
            {
                OnPropertyChanged(nameof(HasItemDetail));
                OnPropertyChanged(nameof(ShowItemList));
            }
        }
    }

    public bool HasItemDetail => SelectedItem is not null;

    public bool ShowItemList => SelectedItem is null;

    public void OpenItem(MiningItemRow item) => SelectedItem = item;

    public void CloseItem() => SelectedItem = null;

    public ObservableCollection<MiningVehicleRow> Vehicles { get; }

    public MiningVehicleRow? SelectedVehicle
    {
        get => _selectedVehicle;
        private set
        {
            if (Set(ref _selectedVehicle, value))
            {
                OnPropertyChanged(nameof(HasVehicleDetail));
                OnPropertyChanged(nameof(ShowVehicleList));
            }
        }
    }

    public bool HasVehicleDetail => SelectedVehicle is not null;

    public bool ShowVehicleList => SelectedVehicle is null;

    public void OpenVehicle(MiningVehicleRow vehicle) => SelectedVehicle = vehicle;

    public void CloseVehicle() => SelectedVehicle = null;

    private void ApplySearch()
    {
        FilteredItems.Clear();
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return;
        }

        foreach (var item in _catalog.Search(SearchText))
        {
            FilteredItems.Add(MiningItemRow.From(item));
        }
    }
}

public sealed class MiningItemRow
{
    private MiningItemRow(string name, string category, string price, string locations, string allLocations, string description)
    {
        Name = name;
        Category = category;
        Price = price;
        Locations = locations;
        AllLocations = allLocations;
        Description = description;
    }

    public string Name { get; }

    public string Category { get; }

    public string Price { get; }

    public string Locations { get; }

    public string AllLocations { get; }

    public string MoreLocations => "Click to show all locations";

    public string Description { get; }

    public static MiningItemRow From(MiningItem item)
    {
        var buys = item.PurchaseLocations
            .Where(offer => offer.Type == "buy")
            .OrderBy(offer => offer.Price ?? long.MaxValue)
            .ThenBy(offer => offer.Verse, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var cheapest = buys.FirstOrDefault(offer => offer.Price is not null);
        var lines = buys.Count == 0
            ? "No buy shop was reported."
            : string.Join(Environment.NewLine, buys.Select(OfferLine));
        var preview = buys.Count == 0
            ? lines
            : string.Join(Environment.NewLine, buys.Take(4).Select(OfferLine));
        return new MiningItemRow(
            item.Name,
            item.Category,
            cheapest?.Price is long price ? FormatAuec(price) : "No shop price reported",
            preview,
            lines,
            item.Description);
    }

    internal static string FormatAuec(long price) =>
        price.ToString("N0", CultureInfo.CurrentCulture) + " aUEC";

    internal static string OfferLine(PurchaseOffer offer)
    {
        var money = offer.Price is long price ? FormatAuec(price) : "price not reported";
        return money + " — " + offer.Verse;
    }

}

public sealed class MiningVehicleRow
{
    private MiningVehicleRow(string name, string category, string capacity, string crew, string price, string buyLines, string rentLines)
    {
        Name = name;
        Category = category;
        Capacity = capacity;
        Crew = crew;
        Price = price;
        BuyLines = buyLines;
        RentLines = rentLines;
    }

    public string Name { get; }

    public string Category { get; }

    public string Capacity { get; }

    public string Crew { get; }

    public string Price { get; }

    public string BuyLines { get; }

    public string RentLines { get; }

    public static MiningVehicleRow From(MiningVehicle vehicle)
    {
        var buy = vehicle.PurchaseLocations.FirstOrDefault(offer => offer.Type == "buy" && offer.Price is not null);
        var capacity = vehicle.OreCapacity is double ore
            ? ore.ToString("0.#", CultureInfo.CurrentCulture) + " SCU"
            : "Ore hold not recorded";
        return new MiningVehicleRow(
            vehicle.Name,
            vehicle.Category,
            capacity,
            "Crew " + vehicle.CrewMin + "–" + vehicle.CrewMax,
            buy?.Price is long price ? MiningItemRow.FormatAuec(price) : "No shop price reported",
            Lines(vehicle, "buy", "No buy shop was reported."),
            Lines(vehicle, "rent", "No rent shop was reported."));
    }

    private static string Lines(MiningVehicle vehicle, string type, string empty)
    {
        var offers = vehicle.PurchaseLocations.Where(offer => offer.Type == type).ToList();
        if (offers.Count == 0)
        {
            return empty;
        }

        return string.Join(Environment.NewLine, offers.Select(MiningItemRow.OfferLine));
    }
}
