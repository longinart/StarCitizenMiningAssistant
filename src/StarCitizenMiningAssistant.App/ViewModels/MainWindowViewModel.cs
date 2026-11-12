using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using StarCitizenMiningAssistant.Core;
using StarCitizenMiningAssistant.Core.Calculation;
using StarCitizenMiningAssistant.Core.Equipment;
using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly MiningCatalog? _catalog;
    private MiningShip? _selectedShip;
    private CatalogChoice<MiningGadget>? _selectedGadget;
    private CatalogChoice<MiningModule>? _selectedActive;
    private string _massText = "5000";
    private string _resistanceText = "0";
    private string _instabilityText = "";
    private string _signatureText = "";
    private string _shipNote = "";
    private string _verdict = "Enter the rock and press Check rock.";
    private string _details = "";

    private MainWindowViewModel(MiningCatalog? catalog, string? catalogError)
    {
        _catalog = catalog;
        WindowTitle = ApplicationInfo.Name;
        ProductName = ApplicationInfo.Name;
        VersionText = "Version " + ApplicationInfo.VersionText;
        CatalogNote = catalog is null
            ? "Equipment catalog did not load. " + catalogError
            : "Equipment " + catalog.GameVersion + ", retrieved " + catalog.Retrieved + ".";

        Ships = new ObservableCollection<MiningShip>(catalog?.Ships ?? []);
        Gadgets = new ObservableCollection<CatalogChoice<MiningGadget>>(GadgetChoices(catalog));
        ActiveModules = new ObservableCollection<CatalogChoice<MiningModule>>(ActiveChoices(catalog));
        ModuleSlotEditor.SetPassiveModules(catalog?.Modules ?? []);
        Turrets =
        [
            new TurretEditor(),
            new TurretEditor(),
            new TurretEditor()
        ];

        _selectedGadget = Gadgets.FirstOrDefault();
        _selectedActive = ActiveModules.FirstOrDefault(choice => choice.Item?.Id == "surge-module")
            ?? ActiveModules.FirstOrDefault();
        _selectedShip = Ships.FirstOrDefault(ship => ship.Id == "misc-prospector") ?? Ships.FirstOrDefault();
        ApplyShip();
    }

    public string WindowTitle { get; }

    public string ProductName { get; }

    public string VersionText { get; }

    public string CatalogNote { get; }

    public ObservableCollection<MiningShip> Ships { get; }

    public ObservableCollection<CatalogChoice<MiningGadget>> Gadgets { get; }

    public ObservableCollection<CatalogChoice<MiningModule>> ActiveModules { get; }

    public IReadOnlyList<TurretEditor> Turrets { get; }

    public MiningShip? SelectedShip
    {
        get => _selectedShip;
        set
        {
            if (Set(ref _selectedShip, value))
            {
                ApplyShip();
            }
        }
    }

    public CatalogChoice<MiningGadget>? SelectedGadget
    {
        get => _selectedGadget;
        set => Set(ref _selectedGadget, value);
    }

    public CatalogChoice<MiningModule>? SelectedActive
    {
        get => _selectedActive;
        set => Set(ref _selectedActive, value);
    }

    public string MassText
    {
        get => _massText;
        set => Set(ref _massText, value);
    }

    public string ResistanceText
    {
        get => _resistanceText;
        set => Set(ref _resistanceText, value);
    }

    public string InstabilityText
    {
        get => _instabilityText;
        set => Set(ref _instabilityText, value);
    }

    public string SignatureText
    {
        get => _signatureText;
        set => Set(ref _signatureText, value);
    }

    public string ShipNote
    {
        get => _shipNote;
        private set => Set(ref _shipNote, value);
    }

    public string Verdict
    {
        get => _verdict;
        private set => Set(ref _verdict, value);
    }

    public string Details
    {
        get => _details;
        private set => Set(ref _details, value);
    }

    public static MainWindowViewModel Create(MiningCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return new MainWindowViewModel(catalog, null);
    }

    public static MainWindowViewModel CreateMissing(string error) =>
        new(null, error);

    public void Check()
    {
        if (_catalog is null)
        {
            Verdict = CatalogNote;
            Details = "";
            return;
        }

        if (!TryReadRequired(MassText, "Mass", out var mass))
        {
            return;
        }

        if (!TryReadRequired(ResistanceText, "Resistance", out var resistance))
        {
            return;
        }

        if (!TryReadOptional(InstabilityText, "Instability", out var instability))
        {
            return;
        }

        if (!TryReadOptional(SignatureText, "Signature", out var signature))
        {
            return;
        }

        if (!TryReadLasers(out var lasers))
        {
            return;
        }

        var rock = new RockReading(
            ObservedValue<double>.Confirmed(mass),
            ObservedValue<double>.Confirmed(resistance),
            instability is null
                ? ObservedValue<double>.Missing()
                : ObservedValue<double>.Confirmed(instability.Value));

        var gadget = SelectedGadget?.Item;
        var active = SelectedActive?.Item;
        ModifierSet? gadgetModifier = gadget is null ? null : ToModifier(gadget);
        ModifierSet? activeModifier = active is null ? null : ToModifier(active);

        var bare = CrackabilityCalculator.Assess(rock, lasers);
        var gadgetOnly = gadgetModifier is null
            ? null
            : CrackabilityCalculator.Assess(rock, lasers, gadgetModifier);
        var consumableOnly = activeModifier is null
            ? null
            : CrackabilityCalculator.Assess(rock, lasers, consumable: activeModifier);
        var both = gadgetModifier is null || activeModifier is null
            ? null
            : CrackabilityCalculator.Assess(rock, lasers, gadgetModifier, activeModifier);

        Verdict = CrackabilityCalculator.DescribeAssistance(
            bare,
            gadgetOnly,
            consumableOnly,
            both,
            gadget?.Name,
            active?.Name);

        var selected = both ?? gadgetOnly ?? consumableOnly ?? bare;
        Details = FormatDetails(bare, gadgetOnly, consumableOnly, both, selected, instability is null, signature);
    }

    private bool TryReadRequired(string text, string label, out double value)
    {
        if (TryParseNumber(text, out value))
        {
            return true;
        }

        Verdict = label + " has to be a number.";
        Details = "";
        value = 0;
        return false;
    }

    private bool TryReadOptional(string text, string label, out double? value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = null;
            return true;
        }

        if (TryParseNumber(text, out var parsed))
        {
            value = parsed;
            return true;
        }

        Verdict = label + " has to be a number, or left blank.";
        Details = "";
        value = null;
        return false;
    }

    private bool TryReadLasers(out List<LaserContribution> lasers)
    {
        lasers = [];
        var anyActive = false;
        foreach (var turret in Turrets)
        {
            if (!turret.IsActive)
            {
                continue;
            }

            anyActive = true;
            if (turret.SelectedHead is null)
            {
                Verdict = "Choose a mining head for turret " + turret.Number + ".";
                Details = "";
                return false;
            }

            if (!TryParseNumber(string.IsNullOrWhiteSpace(turret.CraftPercentText) ? "0" : turret.CraftPercentText, out var craft))
            {
                Verdict = "Craft percent on turret " + turret.Number + " has to be a number.";
                Details = "";
                return false;
            }

            var modules = new List<ModifierSet>();
            foreach (var slot in turret.Slots)
            {
                if (slot.IsEnabled && slot.Selected?.Item is MiningModule module)
                {
                    modules.Add(ToModifier(module));
                }
            }

            var head = turret.SelectedHead;
            lasers.Add(new LaserContribution(
                head.MaxPower,
                craft,
                new ModifierSet(0, head.Resistance, head.Instability, head.GreenZone, head.OptimalChargeRate, 0),
                modules));
        }

        if (!anyActive)
        {
            Verdict = "Choose a ship.";
            Details = "";
            return false;
        }

        return true;
    }

    private void ApplyShip()
    {
        ShipNote = SelectedShip?.VariantOf is null
            ? ""
            : "This variant uses the base hull turret layout. Its own hardpoints were not walked.";

        var ports = SelectedShip?.Turrets;
        for (var i = 0; i < Turrets.Count; i++)
        {
            if (ports is not null && i < ports.Count && _catalog is not null)
            {
                var port = ports[i];
                var heads = _catalog.Heads.Where(head => head.Size == port.Size).ToList();
                var stock = heads.FirstOrDefault(head => head.Id == port.StockHeadId) ?? heads.FirstOrDefault();
                Turrets[i].Activate(i + 1, port.Size, heads, stock);
            }
            else
            {
                Turrets[i].Deactivate();
            }
        }
    }

    private string FormatDetails(
        CrackAssessment bare,
        CrackAssessment? gadgetOnly,
        CrackAssessment? consumableOnly,
        CrackAssessment? both,
        CrackAssessment selected,
        bool instabilityMissing,
        double? signature)
    {
        var text = new StringBuilder();
        text.AppendLine(Line("Lasers only", bare));
        text.AppendLine(gadgetOnly is null ? "Gadget only: not selected" : Line("Gadget only", gadgetOnly));
        text.AppendLine(consumableOnly is null ? "Active module only: not selected" : Line("Active module only", consumableOnly));
        text.AppendLine(both is null ? "Both: not selected" : Line("Both", both));
        text.AppendLine();
        text.AppendLine("Selected loadout");
        if (selected.Band == CrackBand.Unknown)
        {
            text.AppendLine(selected.Reason);
            AppendSignature(text, signature);
            return text.ToString().TrimEnd();
        }

        text.AppendLine("Band: " + BandName(selected.Band));
        text.AppendLine("Required: " + Format(selected.PowerRequired) + "    Delivered: " + Format(selected.PowerDelivered));
        text.AppendLine("Ratio: " + FormatPercent(selected.PowerRatio) + "    Effective resistance: " + Format(selected.EffectiveResistance));
        if (selected.RecommendedThrottlePercent is int throttle)
        {
            text.AppendLine("Suggested throttle: " + throttle + "%");
        }

        if (selected.EstimatedCrackTimeSeconds is double seconds)
        {
            text.AppendLine("Estimated crack time: " + seconds.ToString("0.0", CultureInfo.CurrentCulture) + " s");
        }

        if (selected.MaxCrackableMass is int mass)
        {
            text.AppendLine("Max mass at this resistance: " + mass.ToString("N0", CultureInfo.CurrentCulture) + " kg");
        }

        if (instabilityMissing)
        {
            text.AppendLine("Instability was left blank, so it is not part of this check.");
        }
        else if (selected.EffectiveInstability is double instability)
        {
            text.AppendLine("Effective instability: " + instability.ToString("0.0", CultureInfo.CurrentCulture));
        }

        AppendSignature(text, signature);
        text.Append("Formula " + selected.FormulaVersion);
        return text.ToString();
    }

    private void AppendSignature(StringBuilder text, double? signature)
    {
        if (signature is not double reading || _catalog is null)
        {
            return;
        }

        var matches = _catalog.MatchSignature(reading);
        text.AppendLine(matches.Count == 0
            ? "Signature " + Format(reading) + " is not in the SCMiner table."
            : "Signature " + Format(reading) + ": " + string.Join("; ", matches.Select(DescribeMatch)));
    }

    private static string DescribeMatch(SignatureMatch match)
    {
        var deposit = match.FromDepositLadder ? "deposit " + match.DepositNumber : "base signature";
        return match.Material.Name + " (" + match.Material.Category + ", " + deposit + ")";
    }

    private static string Line(string label, CrackAssessment assessment)
    {
        if (assessment.CanCrack is null || assessment.PowerRatio is null)
        {
            return label + ": " + assessment.Reason;
        }

        var answer = assessment.CanCrack.Value ? "yes" : "no";
        return label + ": " + answer + ", " + FormatPercent(assessment.PowerRatio) + ", " + BandName(assessment.Band);
    }

    private static string BandName(CrackBand band) => band switch
    {
        CrackBand.Trivial => "Trivial",
        CrackBand.Comfortable => "Comfortable",
        CrackBand.Borderline => "Borderline",
        CrackBand.NeedsGadgetOrConsumable => "Needs a gadget or an active module",
        CrackBand.NeedsMoreLasers => "Needs more lasers",
        _ => "Unknown"
    };

    private static string Format(double? value) =>
        value?.ToString("0.##", CultureInfo.CurrentCulture) ?? "—";

    private static string FormatPercent(double? ratio) =>
        ratio is null ? "—" : (ratio.Value * 100).ToString("0", CultureInfo.CurrentCulture) + "%";

    private static bool TryParseNumber(string text, out double value)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
        {
            return true;
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static ModifierSet ToModifier(MiningModule module) =>
        new(module.PowerPercent, module.Resistance, module.Instability, module.GreenZone, module.OptimalChargeRate, module.Shatter);

    private static ModifierSet ToModifier(MiningGadget gadget) =>
        new(gadget.PowerPercent, gadget.Resistance, gadget.Instability, gadget.GreenZone, gadget.OptimalChargeRate, 0);

    private static IEnumerable<CatalogChoice<MiningGadget>> GadgetChoices(MiningCatalog? catalog)
    {
        yield return new CatalogChoice<MiningGadget>("None", null);
        if (catalog is null)
        {
            yield break;
        }

        foreach (var gadget in catalog.Gadgets.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            yield return new CatalogChoice<MiningGadget>(gadget.Name, gadget);
        }
    }

    private static IEnumerable<CatalogChoice<MiningModule>> ActiveChoices(MiningCatalog? catalog)
    {
        yield return new CatalogChoice<MiningModule>("None", null);
        if (catalog is null)
        {
            yield break;
        }

        foreach (var module in catalog.Modules.Where(item => item.Active).OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            yield return new CatalogChoice<MiningModule>(module.Name, module);
        }
    }
}

public sealed class CatalogChoice<T>
    where T : class
{
    public CatalogChoice(string name, T? item)
    {
        Name = name;
        Item = item;
    }

    public string Name { get; }

    public T? Item { get; }
}

public sealed class TurretEditor : ViewModelBase
{
    private readonly ModuleSlotEditor[] _slots = [new(1), new(2), new(3)];
    private bool _isActive;
    private int _number;
    private int _size;
    private MiningHead? _selectedHead;
    private string _craftPercentText = "0";

    public TurretEditor()
    {
        Heads = [];
    }

    public bool IsActive
    {
        get => _isActive;
        private set => Set(ref _isActive, value);
    }

    public int Number
    {
        get => _number;
        private set => Set(ref _number, value);
    }

    public int Size
    {
        get => _size;
        private set
        {
            if (Set(ref _size, value))
            {
                OnPropertyChanged(nameof(Heading));
            }
        }
    }

    public string Heading => "Turret " + Number + " · size " + Size;

    public ObservableCollection<MiningHead> Heads { get; }

    public MiningHead? SelectedHead
    {
        get => _selectedHead;
        set
        {
            if (Set(ref _selectedHead, value))
            {
                ApplySlots();
            }
        }
    }

    public string CraftPercentText
    {
        get => _craftPercentText;
        set => Set(ref _craftPercentText, value);
    }

    public IReadOnlyList<ModuleSlotEditor> Slots => _slots;

    public void Activate(int number, int size, IReadOnlyList<MiningHead> heads, MiningHead? stock)
    {
        Number = number;
        Size = size;
        Heads.Clear();
        foreach (var head in heads)
        {
            Heads.Add(head);
        }

        SelectedHead = stock;
        IsActive = true;
        OnPropertyChanged(nameof(Heading));
    }

    public void Deactivate()
    {
        IsActive = false;
        Heads.Clear();
        SelectedHead = null;
    }

    private void ApplySlots()
    {
        var count = SelectedHead?.ModuleSlots ?? 0;
        for (var i = 0; i < _slots.Length; i++)
        {
            _slots[i].IsEnabled = i < count;
        }
    }
}

public sealed class ModuleSlotEditor : ViewModelBase
{
    private bool _isEnabled;
    private CatalogChoice<MiningModule>? _selected;

    public ModuleSlotEditor(int number)
    {
        Number = number;
        _selected = PassiveChoices[0];
    }

    public static IReadOnlyList<CatalogChoice<MiningModule>> PassiveChoices { get; private set; } =
        [new CatalogChoice<MiningModule>("None", null)];

    public int Number { get; }

    public string Label => "Module " + Number;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => Set(ref _isEnabled, value);
    }

    public CatalogChoice<MiningModule>? Selected
    {
        get => _selected;
        set => Set(ref _selected, value);
    }

    public static void SetPassiveModules(IEnumerable<MiningModule> modules)
    {
        var choices = new List<CatalogChoice<MiningModule>>
        {
            new("None", null)
        };
        choices.AddRange(modules
            .Where(module => !module.Active)
            .OrderBy(module => module.Name, StringComparer.OrdinalIgnoreCase)
            .Select(module => new CatalogChoice<MiningModule>(module.Name, module)));
        PassiveChoices = choices;
    }
}

public abstract class ViewModelBase : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    protected bool Set<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(name);
        return true;
    }

    protected void OnPropertyChanged(string? name) =>
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}
