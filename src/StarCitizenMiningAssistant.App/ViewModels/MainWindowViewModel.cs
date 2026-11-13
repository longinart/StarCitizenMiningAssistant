using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Media;
using StarCitizenMiningAssistant.Core;
using StarCitizenMiningAssistant.Core.Calculation;
using StarCitizenMiningAssistant.Core.Equipment;
using StarCitizenMiningAssistant.Core.Recognition;
using StarCitizenMiningAssistant.Infrastructure.Persistence;

namespace StarCitizenMiningAssistant.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly MiningCatalog? _catalog;
    private bool _suspendSave;
    private MiningShip? _selectedShip;
    private CatalogChoice<MiningGadget>? _selectedGadget;
    private string _massText = "5000";
    private string _resistanceText = "0";
    private string _instabilityText = "";
    private string _signatureText = "";
    private string _shipNote = "";
    private string _verdict = "Enter the rock and press Check rock.";
    private string _verdictHeadline = "";
    private Brush _verdictBrush = Brushes.Transparent;
    private string _details = "";
    private string _mineralSearch = "";
    private string _copyStatus = "";

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
        ModuleSlotEditor.SetModules(catalog?.Modules ?? []);
        Turrets =
        [
            new TurretEditor(),
            new TurretEditor(),
            new TurretEditor()
        ];

        _selectedGadget = Gadgets.FirstOrDefault();
        foreach (var turret in Turrets)
        {
            turret.Changed += NotifyLoadout;
        }

        ActiveTurrets = [];
        Minerals = new ObservableCollection<MineralRow>(
            (catalog?.Materials ?? [])
            .OrderBy(material => material.Name, StringComparer.OrdinalIgnoreCase)
            .Select(material => new MineralRow(material, NotifyLoadout)));
        MineralsView = System.Windows.Data.CollectionViewSource.GetDefaultView(Minerals);
        MineralsView.Filter = item => item is MineralRow row && row.Matches(MineralSearch);
    }

    public event EventHandler? LoadoutChanged;

    public string WindowTitle { get; }

    public string ProductName { get; }

    public string VersionText { get; }

    public string CatalogNote { get; }

    public ObservableCollection<MiningShip> Ships { get; }

    public ObservableCollection<CatalogChoice<MiningGadget>> Gadgets { get; }

    public IReadOnlyList<TurretEditor> Turrets { get; }

    public ObservableCollection<TurretEditor> ActiveTurrets { get; }

    public ObservableCollection<MineralRow> Minerals { get; }

    public System.ComponentModel.ICollectionView MineralsView { get; }

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
        set
        {
            if (Set(ref _selectedGadget, value))
            {
                NotifyLoadout();
            }
        }
    }

    public string MassText
    {
        get => _massText;
        set
        {
            if (Set(ref _massText, value))
            {
                NotifyLoadout();
            }
        }
    }

    public string ResistanceText
    {
        get => _resistanceText;
        set
        {
            if (Set(ref _resistanceText, value))
            {
                NotifyLoadout();
            }
        }
    }

    public string InstabilityText
    {
        get => _instabilityText;
        set
        {
            if (Set(ref _instabilityText, value))
            {
                NotifyLoadout();
            }
        }
    }

    public string SignatureText
    {
        get => _signatureText;
        set
        {
            if (Set(ref _signatureText, value))
            {
                NotifyLoadout();
            }
        }
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

    public string VerdictHeadline
    {
        get => _verdictHeadline;
        private set => Set(ref _verdictHeadline, value);
    }

    public Brush VerdictBrush
    {
        get => _verdictBrush;
        private set => Set(ref _verdictBrush, value);
    }

    public string Details
    {
        get => _details;
        private set => Set(ref _details, value);
    }

    public string MineralSearch
    {
        get => _mineralSearch;
        set
        {
            if (Set(ref _mineralSearch, value))
            {
                MineralsView.Refresh();
            }
        }
    }

    public string CopyStatus
    {
        get => _copyStatus;
        private set => Set(ref _copyStatus, value);
    }

    public void NoteCopied(string mineralName)
    {
        CopyStatus = "Copied " + mineralName + " signatures.";
    }

    public static MainWindowViewModel Create(MiningCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return new MainWindowViewModel(catalog, null);
    }

    public static MainWindowViewModel CreateMissing(string error) =>
        new(null, error);

    public void SelectBaseShip(string shipId)
    {
        var ship = Ships.FirstOrDefault(item => item.Id == shipId);
        if (ship is null)
        {
            return;
        }

        SelectedShip = ship;
    }

    public SavedLoadout CaptureLoadout() =>
        new()
        {
            ShipId = SelectedShip?.Id ?? "",
            GadgetId = SelectedGadget?.Item?.Id,
            MassText = MassText,
            ResistanceText = ResistanceText,
            InstabilityText = InstabilityText,
            SignatureText = SignatureText,
            Turrets = Turrets.Where(turret => turret.IsActive).Select(turret => turret.Capture()).ToList(),
            TrackedMinerals = Minerals.Where(row => row.IsTracked).Select(row => row.Name).ToList()
        };

    public void Restore(SavedLoadout saved)
    {
        ArgumentNullException.ThrowIfNull(saved);
        _suspendSave = true;
        try
        {
            SelectBaseShip(saved.ShipId);
            if (SelectedShip?.Id != saved.ShipId)
            {
                return;
            }

            MassText = string.IsNullOrWhiteSpace(saved.MassText) ? MassText : saved.MassText;
            ResistanceText = string.IsNullOrWhiteSpace(saved.ResistanceText) ? ResistanceText : saved.ResistanceText;
            InstabilityText = saved.InstabilityText ?? "";
            SignatureText = saved.SignatureText ?? "";
            SelectedGadget = Gadgets.FirstOrDefault(choice => choice.Item?.Id == saved.GadgetId) ?? Gadgets.FirstOrDefault();
            var active = Turrets.Where(turret => turret.IsActive).ToList();
            for (var i = 0; i < active.Count && i < saved.Turrets.Count; i++)
            {
                active[i].Apply(saved.Turrets[i]);
            }

            var tracked = new HashSet<string>(saved.TrackedMinerals ?? [], StringComparer.OrdinalIgnoreCase);
            foreach (var row in Minerals)
            {
                row.SetTracked(tracked.Contains(row.Name));
            }
        }
        finally
        {
            _suspendSave = false;
        }
    }

    public void CommitLoadout() => NotifyLoadout();

    private void NotifyLoadout()
    {
        if (_suspendSave || SelectedShip is null)
        {
            return;
        }

        LoadoutChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Check()
    {
        if (_catalog is null)
        {
            ShowMessage(CatalogNote);
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

        if (!TryReadLasers(out var equipped, out var withoutActives, out var activeNames))
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
        ModifierSet? gadgetModifier = gadget is null ? null : ToModifier(gadget);
        var activesSelected = activeNames.Count > 0;

        var bare = CrackabilityCalculator.Assess(rock, withoutActives);
        var gadgetOnly = gadgetModifier is null
            ? null
            : CrackabilityCalculator.Assess(rock, withoutActives, gadgetModifier);
        var consumableOnly = activesSelected
            ? CrackabilityCalculator.Assess(rock, equipped)
            : null;
        var both = activesSelected && gadgetModifier is not null
            ? CrackabilityCalculator.Assess(rock, equipped, gadgetModifier)
            : null;
        var selected = both ?? (activesSelected ? consumableOnly : gadgetOnly) ?? bare;
        var advice = CrackabilityCalculator.DescribeAssistance(
            bare,
            gadgetOnly,
            consumableOnly,
            both,
            gadget?.Name,
            activeNames.Count == 0 ? null : string.Join(", ", activeNames));

        ShowResult(selected, equipped.Count, advice);
        Details = FormatDetails(bare, gadgetOnly, consumableOnly, both, selected!, instability is null, signature);
    }

    private bool TryReadRequired(string text, string label, out double value)
    {
        if (TryParseNumber(text, out value))
        {
            return true;
        }

        ShowMessage(label + " has to be a number.");
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

        ShowMessage(label + " has to be a number, or left blank.");
        Details = "";
        value = null;
        return false;
    }

    private bool TryReadLasers(
        out List<LaserContribution> equipped,
        out List<LaserContribution> withoutActives,
        out List<string> activeNames)
    {
        equipped = [];
        withoutActives = [];
        activeNames = [];
        var anyPort = false;
        foreach (var turret in Turrets)
        {
            if (!turret.IsActive)
            {
                continue;
            }

            anyPort = true;
            if (!turret.IsLaserOn)
            {
                continue;
            }

            if (turret.SelectedHead is null)
            {
                ShowMessage("Choose a mining head for turret " + turret.Number + ".");
                Details = "";
                return false;
            }

            if (!TryParseNumber(string.IsNullOrWhiteSpace(turret.CraftPercentText) ? "0" : turret.CraftPercentText, out var craft))
            {
                ShowMessage("Craft percent on turret " + turret.Number + " has to be a number.");
                Details = "";
                return false;
            }

            var allModules = new List<ModifierSet>();
            var passiveModules = new List<ModifierSet>();
            foreach (var slot in turret.Slots)
            {
                if (!slot.IsEnabled || slot.Selected is not { IsHeader: false, Item: MiningModule module })
                {
                    continue;
                }

                var modifier = ToModifier(module);
                allModules.Add(modifier);
                if (module.Active)
                {
                    activeNames.Add(module.Name);
                }
                else
                {
                    passiveModules.Add(modifier);
                }
            }

            var head = turret.SelectedHead;
            var headModifier = new ModifierSet(0, head.Resistance, head.Instability, head.GreenZone, head.OptimalChargeRate, 0);
            equipped.Add(new LaserContribution(head.MaxPower, craft, headModifier, allModules));
            withoutActives.Add(new LaserContribution(head.MaxPower, craft, headModifier, passiveModules));
        }

        if (!anyPort)
        {
            ShowMessage("Choose a ship.");
            Details = "";
            return false;
        }

        if (equipped.Count == 0)
        {
            ShowMessage("Turn on at least one laser. Extra turrets start off so a one-laser check does not include the whole ship.");
            Details = "";
            return false;
        }

        return true;
    }

    private void ShowMessage(string text)
    {
        VerdictHeadline = "";
        Verdict = text;
    }

    private void ShowResult(CrackAssessment selected, int laserCount, string advice)
    {
        if (selected.CanCrack == true)
        {
            VerdictHeadline = "Crackable";
            VerdictBrush = CrackableBrush;
        }
        else if (selected.PowerRatio is >= 0.7)
        {
            VerdictHeadline = "Difficult";
            VerdictBrush = DifficultBrush;
        }
        else if (selected.PowerRatio is not null)
        {
            VerdictHeadline = "Uncrackable";
            VerdictBrush = UncrackableBrush;
        }
        else
        {
            VerdictHeadline = "";
        }

        Verdict = RatioLine(selected, laserCount) + Environment.NewLine + advice;
    }

    private static readonly SolidColorBrush CrackableBrush = Freeze("#FF3DDC7A");

    private static readonly SolidColorBrush DifficultBrush = Freeze("#FFF0C14A");

    private static readonly SolidColorBrush UncrackableBrush = Freeze("#FFE15B64");

    private static SolidColorBrush Freeze(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    private static string RatioLine(CrackAssessment selected, int laserCount)
    {
        if (selected.PowerRatio is not double ratio
            || selected.PowerDelivered is not double delivered
            || selected.PowerRequired is not double required)
        {
            return selected.Reason;
        }

        var lasers = laserCount == 1 ? "1 laser counted." : laserCount + " lasers counted.";
        return "Power ratio " + FormatPercent(ratio) + ". Delivered "
            + delivered.ToString("N0", CultureInfo.CurrentCulture) + " W against "
            + required.ToString("N0", CultureInfo.CurrentCulture) + " W required. " + lasers;
    }

    private void ApplyShip()
    {
        var variant = SelectedShip?.VariantOf is null
            ? ""
            : "This variant uses the base hull turret layout. Its own hardpoints were not walked. ";
        var extra = SelectedShip?.Turrets.Count > 1
            ? "Extra turrets start off. Turn on each laser that is actually mining."
            : "";
        ShipNote = (variant + extra).Trim();

        var ports = SelectedShip?.Turrets;
        for (var i = 0; i < Turrets.Count; i++)
        {
            if (ports is not null && i < ports.Count && _catalog is not null)
            {
                var port = ports[i];
                var heads = _catalog.Heads.Where(head => head.Size == port.Size).ToList();
                var stock = heads.FirstOrDefault(head => head.Id == port.StockHeadId) ?? heads.FirstOrDefault();
                Turrets[i].Activate(i + 1, port.Size, heads, stock, countThisLaser: i == 0, showLaserSwitch: ports.Count > 1);
            }
            else
            {
                Turrets[i].Deactivate();
            }
        }

        ActiveTurrets.Clear();
        foreach (var turret in Turrets.Where(turret => turret.IsActive))
        {
            ActiveTurrets.Add(turret);
        }

        NotifyLoadout();
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
}

public sealed class CatalogChoice<T>
    where T : class
{
    public CatalogChoice(string name, T? item, bool isHeader = false)
    {
        Name = name;
        Item = item;
        IsHeader = isHeader;
    }

    public string Name { get; }

    public T? Item { get; }

    public bool IsHeader { get; }
}

public sealed class TurretEditor : ViewModelBase
{
    private readonly ModuleSlotEditor[] _slots = [new(1), new(2), new(3)];
    private bool _isActive;
    private bool _isLaserOn;
    private bool _showsLaserSwitch;
    private int _number;
    private int _size;
    private MiningHead? _selectedHead;
    private string _craftPercentText = "0";

    public TurretEditor()
    {
        Heads = [];
        foreach (var slot in _slots)
        {
            slot.Changed += () => Changed?.Invoke();
        }
    }

    public bool IsActive
    {
        get => _isActive;
        private set => Set(ref _isActive, value);
    }

    public bool IsLaserOn
    {
        get => _isLaserOn;
        set
        {
            if (Set(ref _isLaserOn, value))
            {
                Changed?.Invoke();
            }
        }
    }

    public event Action? Changed;

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

    public string Heading => "Turret " + Number;

    public bool ShowsLaserSwitch
    {
        get => _showsLaserSwitch;
        private set => Set(ref _showsLaserSwitch, value);
    }

    public ObservableCollection<MiningHead> Heads { get; }

    public MiningHead? SelectedHead
    {
        get => _selectedHead;
        set
        {
            if (Set(ref _selectedHead, value))
            {
                ApplySlots();
                Changed?.Invoke();
            }
        }
    }

    public SavedTurret Capture() =>
        new()
        {
            HeadId = SelectedHead?.Id,
            LaserOn = IsLaserOn,
            CraftPercent = CraftPercentText,
            Modules = Slots.Select(slot => slot.IsEnabled ? slot.Selected?.Item?.Id : null).ToList()
        };

    public void Apply(SavedTurret saved)
    {
        var head = Heads.FirstOrDefault(item => item.Id == saved.HeadId);
        if (head is not null)
        {
            SelectedHead = head;
        }

        IsLaserOn = saved.LaserOn;
        CraftPercentText = string.IsNullOrWhiteSpace(saved.CraftPercent) ? "0" : saved.CraftPercent;
        var modules = saved.Modules ?? [];
        for (var i = 0; i < Slots.Count && i < modules.Count; i++)
        {
            var moduleId = modules[i];
            Slots[i].Selected = ModuleSlotEditor.ModuleChoices.FirstOrDefault(choice => choice.Item?.Id == moduleId)
                ?? ModuleSlotEditor.ModuleChoices[0];
        }
    }

    public string CraftPercentText
    {
        get => _craftPercentText;
        set
        {
            if (Set(ref _craftPercentText, value))
            {
                Changed?.Invoke();
            }
        }
    }

    public IReadOnlyList<ModuleSlotEditor> Slots => _slots;

    public void Activate(int number, int size, IReadOnlyList<MiningHead> heads, MiningHead? stock, bool countThisLaser, bool showLaserSwitch)
    {
        Number = number;
        Size = size;
        ShowsLaserSwitch = showLaserSwitch;
        Heads.Clear();
        foreach (var head in heads)
        {
            Heads.Add(head);
        }

        SelectedHead = stock;
        IsActive = true;
        IsLaserOn = countThisLaser;
        OnPropertyChanged(nameof(Heading));
    }

    public void Deactivate()
    {
        IsActive = false;
        IsLaserOn = false;
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
        _selected = ModuleChoices[0];
    }

    public static IReadOnlyList<CatalogChoice<MiningModule>> ModuleChoices { get; private set; } =
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
        set
        {
            if (Set(ref _selected, value))
            {
                Changed?.Invoke();
            }
        }
    }

    public event Action? Changed;

    public static void SetModules(IEnumerable<MiningModule> modules)
    {
        var list = modules.ToList();
        var choices = new List<CatalogChoice<MiningModule>>
        {
            new("None", null),
            new("--- passive modules ---", null, true)
        };
        choices.AddRange(list
            .Where(module => !module.Active)
            .OrderBy(module => module.Name, StringComparer.OrdinalIgnoreCase)
            .Select(module => new CatalogChoice<MiningModule>(module.Name, module)));
        choices.Add(new CatalogChoice<MiningModule>("--- active modules ---", null, true));
        choices.AddRange(list
            .Where(module => module.Active)
            .OrderBy(module => module.Name, StringComparer.OrdinalIgnoreCase)
            .Select(module => new CatalogChoice<MiningModule>(module.Name, module)));
        ModuleChoices = choices;
    }
}

public sealed class MineralRow : ViewModelBase
{
    private readonly Action _changed;
    private bool _isTracked;
    private bool _isExpanded;

    public MineralRow(MineableMaterial material, Action changed)
    {
        ArgumentNullException.ThrowIfNull(material);
        _changed = changed;
        Name = material.Name;
        var numbers = material.DepositSignatures.Where(value => value > 0).ToList();
        if (numbers.Count == 0 && material.Signature > 0)
        {
            numbers.Add(material.Signature);
        }

        Numbers = numbers;
    }

    public string Name { get; }

    public IReadOnlyList<double> Numbers { get; }

    public bool HasSignatures => Numbers.Count > 0;

    public string SignatureLine => string.Join("    ", Numbers.Select(FormatNumber));

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (Set(ref _isExpanded, value))
            {
                OnPropertyChanged(nameof(ShowSignatures));
                OnPropertyChanged(nameof(ShowMissing));
            }
        }
    }

    public bool ShowSignatures => IsExpanded && HasSignatures;

    public bool ShowMissing => IsExpanded && !HasSignatures;

    public bool IsTracked
    {
        get => _isTracked;
        set
        {
            if (Set(ref _isTracked, value))
            {
                _changed();
            }
        }
    }

    public void SetTracked(bool tracked) => Set(ref _isTracked, tracked);

    public bool Matches(string search) =>
        string.IsNullOrWhiteSpace(search)
        || Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string FormatNumber(double value) =>
        value == Math.Truncate(value)
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);
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
