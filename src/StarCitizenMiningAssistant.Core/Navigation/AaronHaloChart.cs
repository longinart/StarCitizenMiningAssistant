using System.Globalization;
using System.Text.Json;

namespace StarCitizenMiningAssistant.Core.Navigation;

public sealed class AaronHaloPlace
{
    public AaronHaloPlace(string id, string name, string shortName, string? note)
    {
        Id = id;
        Name = name;
        ShortName = shortName;
        Note = string.IsNullOrWhiteSpace(note) ? null : note;
    }

    public string Id { get; }

    public string Name { get; }

    public string ShortName { get; }

    public string? Note { get; }
}

public sealed class AaronHaloRoute
{
    public AaronHaloRoute(
        string destinationShort,
        string exit,
        string margin,
        string? note,
        string? reverseName,
        string? reverseExit,
        string? reverseMargin,
        string? reverseNote,
        bool recommended)
    {
        DestinationShort = destinationShort;
        Exit = exit;
        Margin = margin;
        Note = note;
        ReverseName = reverseName;
        ReverseExit = reverseExit;
        ReverseMargin = reverseMargin;
        ReverseNote = reverseNote;
        Recommended = recommended;
    }

    public string DestinationShort { get; }

    public string Exit { get; }

    public string Margin { get; }

    public string? Note { get; }

    public string? ReverseName { get; }

    public string? ReverseExit { get; }

    public string? ReverseMargin { get; }

    public string? ReverseNote { get; }

    public bool HasReverse => ReverseName is not null;

    public bool HasNote => Note is not null;

    public bool Recommended { get; }

    public string RecommendedLabel => Recommended ? "Recommended" : "";
}

/// <summary>
/// BandHopper's precomputed Aaron Halo Band 5 exits. The margins are stored, not recalculated.
/// </summary>
public sealed class AaronHaloChart
{
    public static AaronHaloChart Empty { get; } = new([], []);

    private readonly Dictionary<string, AaronHaloPlace> _places;
    private readonly List<StoredRoute> _routes;

    private AaronHaloChart(List<AaronHaloPlace> places, List<StoredRoute> routes)
    {
        _places = places.ToDictionary(place => place.Id, StringComparer.OrdinalIgnoreCase);
        _routes = routes;
        Homes = _routes
            .Select(route => route.From)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(id => _places[id])
            .OrderBy(place => place.ShortName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<AaronHaloPlace> Homes { get; }

    public IReadOnlyList<AaronHaloRoute> From(string? homeId)
    {
        if (string.IsNullOrWhiteSpace(homeId) || !_places.ContainsKey(homeId))
        {
            return [];
        }

        var ranked = _routes
            .Where(route => string.Equals(route.From, homeId, StringComparison.OrdinalIgnoreCase))
            .Select(route => (route, place: _places[route.To]))
            .OrderByDescending(item => item.route.MarginKm)
            .ThenBy(item => item.place.ShortName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return ranked.Select((item, index) =>
        {
            var back = _routes.FirstOrDefault(route =>
                string.Equals(route.From, item.route.To, StringComparison.OrdinalIgnoreCase)
                && string.Equals(route.To, item.route.From, StringComparison.OrdinalIgnoreCase));
            var backPlace = back is null ? null : _places[back.To];
            return new AaronHaloRoute(
                item.place.ShortName,
                "Exit quantum at " + FormatExit(item.route.DistanceToDestinationKm),
                "Margin: " + FormatMargin(item.route.MarginKm),
                item.place.Note,
                backPlace?.ShortName,
                back is null ? null : "Exit quantum at " + FormatExit(back.DistanceToDestinationKm),
                back is null ? null : "Margin: " + FormatMargin(back.MarginKm),
                backPlace?.Note,
                index == 0);
        }).ToList();
    }

    public static AaronHaloChart Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var file = JsonSerializer.Deserialize<FileModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new FormatException("The Aaron Halo chart was empty.");
        if (file.Places is null || file.Routes is null || file.Places.Count == 0 || file.Routes.Count == 0)
        {
            throw new FormatException("The Aaron Halo chart has no routes.");
        }

        if (file.BandId != 5)
        {
            throw new FormatException("The Aaron Halo chart is not Band 5.");
        }

        var places = file.Places.Select(place => new AaronHaloPlace(
            Required(place.Id, "place"),
            Required(place.Name, "place name"),
            Required(place.ShortName, "place short name"),
            place.Note)).ToList();
        var known = places.Select(place => place.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var routes = file.Routes.Select(route =>
        {
            var from = Required(route.From, "route start");
            var to = Required(route.To, "route destination");
            if (!known.Contains(from) || !known.Contains(to))
            {
                throw new FormatException("A route names a place that is not in the chart.");
            }

            if (route.DistanceToDestinationKm <= 0 || route.MarginKm < 0)
            {
                throw new FormatException("A route has a distance or margin that is not usable.");
            }

            return new StoredRoute(from, to, route.DistanceToDestinationKm, route.MarginKm);
        }).ToList();
        return new AaronHaloChart(places, routes);
    }

    public static string FormatExit(int kilometres)
    {
        var gigametres = kilometres / 1_000_000d;
        if (gigametres >= 1)
        {
            return gigametres.ToString("0.0", CultureInfo.InvariantCulture) + " Gm";
        }

        var megametres = kilometres / 1_000d;
        return megametres >= 100
            ? Math.Round(megametres).ToString("0", CultureInfo.InvariantCulture) + " Mm"
            : megametres.ToString("0.0", CultureInfo.InvariantCulture) + " Mm";
    }

    public static string FormatMargin(int kilometres) =>
        Math.Round(kilometres / 1_000d).ToString("0", CultureInfo.InvariantCulture) + " Mm";

    private static string Required(string? value, string label) =>
        string.IsNullOrWhiteSpace(value) ? throw new FormatException("The Aaron Halo chart is missing a " + label + ".") : value;

    private sealed record StoredRoute(string From, string To, int DistanceToDestinationKm, int MarginKm);

    private sealed class FileModel
    {
        public int BandId { get; set; }

        public List<PlaceModel>? Places { get; set; }

        public List<RouteModel>? Routes { get; set; }
    }

    private sealed class PlaceModel
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? ShortName { get; set; }

        public string? Note { get; set; }
    }

    private sealed class RouteModel
    {
        public string? From { get; set; }

        public string? To { get; set; }

        public int DistanceToDestinationKm { get; set; }

        public int MarginKm { get; set; }
    }
}
