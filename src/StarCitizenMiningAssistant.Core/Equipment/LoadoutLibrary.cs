using System.Text.Json;
using StarCitizenMiningAssistant.Core.Capture;

namespace StarCitizenMiningAssistant.Core.Equipment;

public sealed class StoredTurret
{
    public string? HeadId { get; set; }

    public bool LaserOn { get; set; }

    public string CraftPercent { get; set; } = "0";

    public List<string?> Modules { get; set; } = [];
}

public sealed class StoredLoadout
{
    public string Name { get; set; } = "";

    public string ShipId { get; set; } = "";

    public string? GadgetId { get; set; }

    public List<StoredTurret> Turrets { get; set; } = [];
}

/// <summary>
/// Named ship setups plus the screen that was open last. Saving one name
/// replaces only that name. The draft is restored on the next start.
/// </summary>
public sealed class LoadoutLibrary
{
    public const string LegacyName = "Loadout";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public string? ActiveName { get; set; }

    public StoredLoadout? Draft { get; set; }

    public List<StoredLoadout> Loadouts { get; set; } = [];

    public List<string> TrackedMinerals { get; set; } = [];

    public string MassText { get; set; } = "5000";

    public string ResistanceText { get; set; } = "0";

    public string InstabilityText { get; set; } = "";

    public string SignatureText { get; set; } = "";

    public double? OverlayLeft { get; set; }

    public double? OverlayTop { get; set; }

    public double? OverlayWidth { get; set; }

    public double? OverlayHeight { get; set; }

    public List<MonitorCapture> CaptureMonitors { get; set; } = [];

    public static LoadoutLibrary FromLegacy(
        StoredLoadout setup,
        IReadOnlyList<string>? trackedMinerals,
        string massText,
        string resistanceText,
        string instabilityText,
        string signatureText)
    {
        var named = Clone(setup);
        named.Name = string.IsNullOrWhiteSpace(setup.Name) ? LegacyName : setup.Name.Trim();
        return new LoadoutLibrary
        {
            ActiveName = named.Name,
            Draft = Clone(named),
            Loadouts = [named],
            TrackedMinerals = trackedMinerals?.ToList() ?? [],
            MassText = string.IsNullOrWhiteSpace(massText) ? "5000" : massText,
            ResistanceText = string.IsNullOrWhiteSpace(resistanceText) ? "0" : resistanceText,
            InstabilityText = instabilityText ?? "",
            SignatureText = signatureText ?? ""
        };
    }

    public void RememberDraft(StoredLoadout screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var draft = Clone(screen);
        draft.Name = ActiveName ?? "";
        Draft = draft;
    }

    public bool SaveNamed(string name, StoredLoadout screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        var stored = Clone(screen);
        stored.Name = trimmed;
        var index = IndexOf(trimmed);
        if (index >= 0)
        {
            Loadouts[index] = stored;
        }
        else
        {
            Loadouts.Add(stored);
        }

        ActiveName = trimmed;
        Draft = Clone(stored);
        return true;
    }

    public StoredLoadout? Find(string? name)
    {
        var index = IndexOf(name);
        return index < 0 ? null : Loadouts[index];
    }

    public bool Delete(string? name)
    {
        var index = IndexOf(name);
        if (index < 0)
        {
            return false;
        }

        Loadouts.RemoveAt(index);
        if (string.Equals(ActiveName, name?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            ActiveName = null;
        }

        return true;
    }

    public bool RememberCapture(string? deviceName, CaptureTarget target, CaptureRectangle rectangle)
    {
        ArgumentNullException.ThrowIfNull(rectangle);
        var name = deviceName?.Trim() ?? "";
        if (name.Length == 0 || !rectangle.IsPlaceable)
        {
            return false;
        }

        var monitor = CaptureMonitors.FirstOrDefault(item =>
            string.Equals(item.DeviceName, name, StringComparison.OrdinalIgnoreCase));
        if (monitor is null)
        {
            monitor = new MonitorCapture { DeviceName = name };
            CaptureMonitors.Add(monitor);
        }

        var copy = Copy(rectangle);
        if (target == CaptureTarget.Signatures)
        {
            monitor.Signatures = copy;
        }
        else
        {
            monitor.Rock = copy;
        }

        return true;
    }

    public MonitorCapture? FindCapture(string? deviceName)
    {
        var name = deviceName?.Trim() ?? "";
        if (name.Length == 0)
        {
            return null;
        }

        return CaptureMonitors.FirstOrDefault(item =>
            string.Equals(item.DeviceName, name, StringComparison.OrdinalIgnoreCase));
    }

    public static List<MonitorCapture> CloneCaptures(IEnumerable<MonitorCapture>? source) =>
        (source ?? []).Select(monitor => new MonitorCapture
        {
            DeviceName = monitor.DeviceName ?? "",
            Signatures = Copy(monitor.Signatures),
            Rock = Copy(monitor.Rock)
        }).ToList();

    public static StoredLoadout Clone(StoredLoadout source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new StoredLoadout
        {
            Name = source.Name,
            ShipId = source.ShipId,
            GadgetId = source.GadgetId,
            Turrets = (source.Turrets ?? []).Select(turret => new StoredTurret
            {
                HeadId = turret.HeadId,
                LaserOn = turret.LaserOn,
                CraftPercent = turret.CraftPercent,
                Modules = turret.Modules?.ToList() ?? []
            }).ToList()
        };
    }

    public static LoadoutLibrary? TryParse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (HasProperty(root, "loadouts") || HasProperty(root, "draft"))
            {
                return JsonSerializer.Deserialize<LoadoutLibrary>(json, JsonOptions);
            }

            var legacy = JsonSerializer.Deserialize<LegacyFile>(json, JsonOptions);
            if (legacy is null || string.IsNullOrWhiteSpace(legacy.ShipId))
            {
                return null;
            }

            return FromLegacy(
                new StoredLoadout
                {
                    ShipId = legacy.ShipId,
                    GadgetId = legacy.GadgetId,
                    Turrets = legacy.Turrets ?? []
                },
                legacy.TrackedMinerals,
                legacy.MassText ?? "",
                legacy.ResistanceText ?? "",
                legacy.InstabilityText ?? "",
                legacy.SignatureText ?? "");
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    private int IndexOf(string? name)
    {
        var trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            return -1;
        }

        for (var i = 0; i < Loadouts.Count; i++)
        {
            if (string.Equals(Loadouts[i].Name, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static CaptureRectangle? Copy(CaptureRectangle? source) =>
        source is null
            ? null
            : new CaptureRectangle
            {
                Left = source.Left,
                Top = source.Top,
                Width = source.Width,
                Height = source.Height
            };

    private static bool HasProperty(JsonElement root, string name)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class LegacyFile
    {
        public string ShipId { get; set; } = "";

        public string? GadgetId { get; set; }

        public string? MassText { get; set; }

        public string? ResistanceText { get; set; }

        public string? InstabilityText { get; set; }

        public string? SignatureText { get; set; }

        public List<StoredTurret>? Turrets { get; set; }

        public List<string>? TrackedMinerals { get; set; }
    }
}
