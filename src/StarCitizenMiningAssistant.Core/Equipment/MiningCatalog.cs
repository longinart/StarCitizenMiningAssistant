using System.Text.Json;

namespace StarCitizenMiningAssistant.Core.Equipment;

public sealed class MiningCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private MiningCatalog(
        string source,
        string gameVersion,
        string retrieved,
        string signatureSource,
        string statPreference,
        IReadOnlyList<MiningShip> ships,
        IReadOnlyList<MiningHead> heads,
        IReadOnlyList<MiningModule> modules,
        IReadOnlyList<MiningGadget> gadgets,
        IReadOnlyList<MineableMaterial> materials)
    {
        Source = source;
        GameVersion = gameVersion;
        Retrieved = retrieved;
        SignatureSource = signatureSource;
        StatPreference = statPreference;
        Ships = ships;
        Heads = heads;
        Modules = modules;
        Gadgets = gadgets;
        Materials = materials;
    }

    public string Source { get; }

    public string GameVersion { get; }

    public string Retrieved { get; }

    public string SignatureSource { get; }

    public string StatPreference { get; }

    public IReadOnlyList<MiningShip> Ships { get; }

    public IReadOnlyList<MiningHead> Heads { get; }

    public IReadOnlyList<MiningModule> Modules { get; }

    public IReadOnlyList<MiningGadget> Gadgets { get; }

    public IReadOnlyList<MineableMaterial> Materials { get; }

    public MiningHead? FindHead(string id) =>
        Heads.FirstOrDefault(head => head.Id == id);

    public MiningModule? FindModule(string id) =>
        Modules.FirstOrDefault(module => module.Id == id);

    public MiningGadget? FindGadget(string id) =>
        Gadgets.FirstOrDefault(gadget => gadget.Id == id);

    public IReadOnlyList<SignatureMatch> MatchSignature(double reading)
    {
        var matches = new List<SignatureMatch>();
        foreach (var material in Materials)
        {
            if (Nearly(material.Signature, reading))
            {
                matches.Add(new SignatureMatch(material, 1, material.DepositSignatures.Count > 0));
            }

            for (var i = 0; i < material.DepositSignatures.Count; i++)
            {
                if (i == 0 && Nearly(material.Signature, reading))
                {
                    continue;
                }

                if (Nearly(material.DepositSignatures[i], reading))
                {
                    matches.Add(new SignatureMatch(material, i + 1, true));
                }
            }
        }

        return matches;
    }

    private static bool Nearly(double left, double right) => Math.Abs(left - right) < 0.001;

    public static MiningCatalog Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var file = JsonSerializer.Deserialize<CatalogFile>(json, JsonOptions)
            ?? throw new JsonException("The equipment catalog was empty.");

        if (file.Heads is null || file.Heads.Count == 0)
        {
            throw new JsonException("The equipment catalog has no mining heads.");
        }

        return new MiningCatalog(
            file.Source ?? "",
            file.GameVersion ?? "",
            file.Retrieved ?? "",
            file.SignatureSource ?? "",
            file.StatPreference ?? "",
            file.Ships ?? [],
            file.Heads,
            file.Modules ?? [],
            file.Gadgets ?? [],
            file.Materials ?? []);
    }

    private sealed class CatalogFile
    {
        public string? Source { get; set; }

        public string? GameVersion { get; set; }

        public string? Retrieved { get; set; }

        public List<MiningShip>? Ships { get; set; }

        public List<MiningHead>? Heads { get; set; }

        public List<MiningModule>? Modules { get; set; }

        public List<MiningGadget>? Gadgets { get; set; }

        public string? SignatureSource { get; set; }

        public string? StatPreference { get; set; }

        public List<MineableMaterial>? Materials { get; set; }
    }
}

public sealed record SignatureMatch(MineableMaterial Material, int DepositNumber, bool FromDepositLadder);

public sealed class MineableMaterial
{
    public string Name { get; set; } = "";

    public string Category { get; set; } = "";

    public double Signature { get; set; }

    public double Instability { get; set; }

    public double ResistancePercent { get; set; }

    public List<double> DepositSignatures { get; set; } = [];
}

public sealed class MiningShip
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string? VariantOf { get; set; }

    public double OreCapacity { get; set; }

    public int CrewMin { get; set; }

    public int CrewMax { get; set; }

    public List<ShipTurret> Turrets { get; set; } = [];
}

public sealed class ShipTurret
{
    public int Index { get; set; }

    public int Size { get; set; }

    public string StockHeadId { get; set; } = "";
}

public sealed class MiningHead
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public int Size { get; set; }

    public double MinPower { get; set; }

    public double MaxPower { get; set; }

    public int ModuleSlots { get; set; }

    public double OptimalRange { get; set; }

    public double MaximumRange { get; set; }

    public double Resistance { get; set; }

    public double Instability { get; set; }

    public double GreenZone { get; set; }

    public double OptimalChargeRate { get; set; }

    public double AllChargeRates { get; set; }

    public double InertMaterials { get; set; }
}

public sealed class MiningModule
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public bool Active { get; set; }

    public int Charges { get; set; }

    public double? DurationSeconds { get; set; }

    public double PowerPercent { get; set; }

    public double Resistance { get; set; }

    public double Instability { get; set; }

    public double GreenZone { get; set; }

    public double OptimalChargeRate { get; set; }

    public double Shatter { get; set; }

    public double OverchargeRate { get; set; }

    public double InertMaterials { get; set; }

    public double ExtractPowerPercent { get; set; }
}

public sealed class MiningGadget
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public double Resistance { get; set; }

    public double Instability { get; set; }

    public double GreenZone { get; set; }

    public double OptimalChargeRate { get; set; }

    public double PowerPercent { get; set; }

    public double Shatter { get; set; }

    public double Clustering { get; set; }
}
