using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Media;
using StarCitizenMiningAssistant.Core;
using StarCitizenMiningAssistant.Core.Calculation;
using StarCitizenMiningAssistant.Core.Capture;
using StarCitizenMiningAssistant.Core.Equipment;
using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly MiningCatalog? _catalog;
    private bool _suspendSave;
    private MiningShip? _selectedShip;
    private CatalogChoice<MiningGadget>? _selectedGadget;
    private IReadOnlyList<SetupStat> _gadgetStatistics = [];
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
    private string _loadoutName = "";
    private LoadoutNameChoice? _selectedLoadoutName;
    private readonly LoadoutLibrary _library = new();
    private bool _applyingScan;
    private ObservedValue<double>? _lastMassSample;
    private ObservedValue<double>? _lastResistanceSample;
    private ObservedValue<double>? _lastInstabilitySample;
    private IReadOnlyList<OverlayCrackRow> _crackRows = [];
    private IReadOnlyList<OverlaySignatureHit> _signatureHits = [];
    private string _soundKey = "";
    private bool _soundArmed;

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
                RefreshGadgetStatistics();
                NotifyLoadout();
            }
        }
    }

    public IReadOnlyList<SetupStat> GadgetStatistics => _gadgetStatistics;

    public bool HasGadget => SelectedGadget?.Item is not null;

    private void RefreshGadgetStatistics()
    {
        var gadget = SelectedGadget?.Item;
        _gadgetStatistics = gadget is null ? [] : EquipmentStatText.Gadget(gadget);
        OnPropertyChanged(nameof(GadgetStatistics));
        OnPropertyChanged(nameof(HasGadget));
    }

    public string MassText
    {
        get => _massText;
        set
        {
            if (Set(ref _massText, value))
            {
                if (!_applyingScan && !_suspendSave)
                {
                    _library.MassConfidence = "";
                }

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
                if (!_applyingScan && !_suspendSave)
                {
                    _library.ResistanceConfidence = "";
                }

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
                if (!_applyingScan && !_suspendSave)
                {
                    _library.InstabilityConfidence = "";
                }

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
                if (!_applyingScan && !_suspendSave)
                {
                    _library.SignatureConfidence = "";
                }

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

    public ObservableCollection<LoadoutNameChoice> LoadoutNames { get; } = [];

    public string LoadoutName
    {
        get => _loadoutName;
        set => Set(ref _loadoutName, value);
    }

    public LoadoutNameChoice? SelectedLoadoutName
    {
        get => _selectedLoadoutName;
        private set => Set(ref _selectedLoadoutName, value);
    }

    public LoadoutLibrary CaptureLibrary()
    {
        _library.RememberDraft(CaptureScreen());
        _library.TrackedMinerals = Minerals.Where(row => row.IsTracked).Select(row => row.Name).ToList();
        _library.MassText = MassText;
        _library.ResistanceText = ResistanceText;
        _library.InstabilityText = InstabilityText;
        _library.SignatureText = SignatureText;
        return _library;
    }

    public void RestoreLibrary(LoadoutLibrary saved)
    {
        ArgumentNullException.ThrowIfNull(saved);
        _suspendSave = true;
        try
        {
            _library.Loadouts = saved.Loadouts.Select(LoadoutLibrary.Clone).ToList();
            _library.ActiveName = saved.ActiveName;
            _library.Draft = saved.Draft is null ? null : LoadoutLibrary.Clone(saved.Draft);
            _library.TrackedMinerals = saved.TrackedMinerals?.ToList() ?? [];
            _library.MassText = saved.MassText;
            _library.ResistanceText = saved.ResistanceText;
            _library.InstabilityText = saved.InstabilityText;
            _library.MassConfidence = saved.MassConfidence ?? "";
            _library.ResistanceConfidence = saved.ResistanceConfidence ?? "";
            _library.InstabilityConfidence = saved.InstabilityConfidence ?? "";
            _library.SignatureText = saved.SignatureText;
            _library.SignatureConfidence = saved.SignatureConfidence ?? "";
            _library.SoundEnabled = saved.SoundEnabled;
            _library.ClickThrough = saved.ClickThrough;
            _library.OverlayLeft = saved.OverlayLeft;
            _library.OverlayTop = saved.OverlayTop;
            _library.OverlayWidth = saved.OverlayWidth;
            _library.OverlayHeight = saved.OverlayHeight;
            _library.CaptureMonitors = LoadoutLibrary.CloneCaptures(saved.CaptureMonitors);
            MassText = string.IsNullOrWhiteSpace(saved.MassText) ? MassText : saved.MassText;
            ResistanceText = string.IsNullOrWhiteSpace(saved.ResistanceText) ? ResistanceText : saved.ResistanceText;
            InstabilityText = saved.InstabilityText ?? "";
            SignatureText = saved.SignatureText ?? "";
            OnPropertyChanged(nameof(SoundEnabled));
            OnPropertyChanged(nameof(ClickThrough));

            var screen = _library.Draft ?? _library.Find(_library.ActiveName);
            if (screen is not null)
            {
                ApplyScreen(screen);
                LoadoutName = string.IsNullOrWhiteSpace(_library.ActiveName) ? screen.Name : _library.ActiveName;
            }

            var tracked = new HashSet<string>(_library.TrackedMinerals, StringComparer.OrdinalIgnoreCase);
            foreach (var row in Minerals)
            {
                row.SetTracked(tracked.Contains(row.Name));
            }

            RefreshLoadoutNames();
        }
        finally
        {
            _suspendSave = false;
        }

        RefreshCrackRows();
        RefreshSignatureWatch();
    }

    public bool SaveNamedLoadout()
    {
        if (SelectedShip is null || !_library.SaveNamed(LoadoutName, CaptureScreen()))
        {
            return false;
        }

        LoadoutName = _library.ActiveName ?? LoadoutName;
        RefreshLoadoutNames();
        NotifyLoadout();
        return true;
    }

    public void SelectNamedLoadout(string name)
    {
        if (string.Equals(name, _library.ActiveName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var chosen = _library.Find(name);
        if (chosen is null || SelectedShip is null)
        {
            return;
        }

        _suspendSave = true;
        try
        {
            if (!string.IsNullOrWhiteSpace(_library.ActiveName))
            {
                _library.SaveNamed(_library.ActiveName, CaptureScreen());
            }

            chosen = _library.Find(name);
            if (chosen is null)
            {
                return;
            }

            _library.ActiveName = chosen.Name;
            _library.Draft = LoadoutLibrary.Clone(chosen);
            ApplyScreen(chosen);
            LoadoutName = chosen.Name;
            RefreshLoadoutNames();
        }
        finally
        {
            _suspendSave = false;
        }

        NotifyLoadout();
    }

    public void DeleteNamedLoadout()
    {
        if (!_library.Delete(_library.ActiveName))
        {
            return;
        }

        _library.RememberDraft(CaptureScreen());
        RefreshLoadoutNames();
        NotifyLoadout();
    }

    private StoredLoadout CaptureScreen() =>
        new()
        {
            Name = _library.ActiveName ?? "",
            ShipId = SelectedShip?.Id ?? "",
            GadgetId = SelectedGadget?.Item?.Id,
            Turrets = Turrets.Where(turret => turret.IsActive).Select(turret => turret.Capture()).ToList()
        };

    private void ApplyScreen(StoredLoadout saved)
    {
        if (string.IsNullOrWhiteSpace(saved.ShipId))
        {
            return;
        }

        SelectBaseShip(saved.ShipId);
        if (SelectedShip?.Id != saved.ShipId)
        {
            return;
        }

        SelectedGadget = Gadgets.FirstOrDefault(choice => choice.Item?.Id == saved.GadgetId) ?? Gadgets.FirstOrDefault();
        var active = Turrets.Where(turret => turret.IsActive).ToList();
        var turrets = saved.Turrets ?? [];
        for (var i = 0; i < active.Count && i < turrets.Count; i++)
        {
            active[i].Apply(turrets[i]);
        }
    }

    private void RefreshLoadoutNames()
    {
        var names = _library.Loadouts.Select(loadout => loadout.Name).ToList();
        var unchanged = LoadoutNames.Count == names.Count
            && LoadoutNames.Select(choice => choice.Name).SequenceEqual(names);
        if (!unchanged)
        {
            LoadoutNames.Clear();
            foreach (var name in names)
            {
                LoadoutNames.Add(new LoadoutNameChoice(name));
            }
        }

        SelectedLoadoutName = LoadoutNames.FirstOrDefault(choice =>
            string.Equals(choice.Name, _library.ActiveName, StringComparison.OrdinalIgnoreCase));
    }

    public void CommitLoadout() => NotifyLoadout();

    public IReadOnlyList<OverlayCrackRow> CrackRows => _crackRows;

    public IReadOnlyList<OverlaySignatureHit> SignatureHits => _signatureHits;

    public bool SoundEnabled
    {
        get => _library.SoundEnabled;
        set
        {
            if (_library.SoundEnabled == value)
            {
                return;
            }

            _library.SoundEnabled = value;
            OnPropertyChanged(nameof(SoundEnabled));
            NotifyLoadout();
        }
    }

    public event EventHandler? ClickThroughChanged;

    public bool ClickThrough
    {
        get => _library.ClickThrough;
        set
        {
            if (_library.ClickThrough == value)
            {
                return;
            }

            _library.ClickThrough = value;
            OnPropertyChanged(nameof(ClickThrough));
            ClickThroughChanged?.Invoke(this, EventArgs.Empty);
            NotifyLoadout();
        }
    }

    public string OverlayNote { get; private set; } = "Type the rock here until the scan area can read it.";

    public double? OverlayLeft => _library.OverlayLeft;

    public double? OverlayTop => _library.OverlayTop;

    public double? OverlayWidth => _library.OverlayWidth;

    public double? OverlayHeight => _library.OverlayHeight;

    public void RememberOverlay(double left, double top, double width, double height)
    {
        if (Near(_library.OverlayLeft, left)
            && Near(_library.OverlayTop, top)
            && Near(_library.OverlayWidth, width)
            && Near(_library.OverlayHeight, height))
        {
            return;
        }

        _library.OverlayLeft = left;
        _library.OverlayTop = top;
        _library.OverlayWidth = width;
        _library.OverlayHeight = height;
        NotifyLoadout();
    }

    public bool RememberCapture(string deviceName, CaptureTarget target, CaptureRectangle rectangle)
    {
        if (!_library.RememberCapture(deviceName, target, rectangle))
        {
            return false;
        }

        NotifyLoadout();
        return true;
    }

    public void ApplyRockScan(RockOcrFields fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        _applyingScan = true;
        try
        {
            _library.MassConfidence = RockScan.ConfidenceName(fields.Mass.Confidence);
            _library.ResistanceConfidence = RockScan.ConfidenceName(fields.Resistance.Confidence);
            _library.InstabilityConfidence = RockScan.ConfidenceName(fields.Instability.Confidence);
            MassText = DisplayReading(fields.Mass);
            ResistanceText = DisplayReading(fields.Resistance);
            InstabilityText = DisplayReading(fields.Instability);
        }
        finally
        {
            _applyingScan = false;
        }

        NotifyLoadout();
    }

    public ObservedValue<double> ApplySignatureScan(IReadOnlyList<ObservedValue<string>> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var reading = _catalog is null
            ? ObservedValue<double>.Missing()
            : SignatureReading.FromLines(_catalog, lines);
        ApplySignature(reading);
        return reading;
    }

    public HeldReading WatchSignature(IReadOnlyList<ObservedValue<string>>? lines)
    {
        var sample = lines is null || _catalog is null
            ? ObservedValue<double>.Missing()
            : SignatureReading.FromLines(_catalog, lines);
        var previous = RockScan.Value(SignatureText, _library.SignatureConfidence);
        var held = ReadingHold.Next(previous, sample);
        if (!held.Equals(previous))
        {
            ApplySignature(held);
        }

        return new HeldReading(sample, held);
    }

    public RockOcrFields WatchRock(RockOcrFields? sample)
    {
        var current = ReadRock();
        var mass = StableReading.Next(current.MassKilograms, _lastMassSample, sample?.Mass ?? ObservedValue<double>.Missing());
        var resistance = StableReading.Next(current.ResistancePercent, _lastResistanceSample, sample?.Resistance ?? ObservedValue<double>.Missing());
        var instability = StableReading.Next(current.Instability, _lastInstabilitySample, sample?.Instability ?? ObservedValue<double>.Missing());
        _lastMassSample = mass.LastSample;
        _lastResistanceSample = resistance.LastSample;
        _lastInstabilitySample = instability.LastSample;
        var held = new RockOcrFields
        {
            Mass = mass.Shown,
            Resistance = resistance.Shown,
            Instability = instability.Shown
        };
        if (!held.Mass.Equals(current.MassKilograms)
            || !held.Resistance.Equals(current.ResistancePercent)
            || !held.Instability.Equals(current.Instability))
        {
            ApplyRockScan(held);
        }

        return held;
    }

    private void ApplySignature(ObservedValue<double> reading)
    {
        _applyingScan = true;
        try
        {
            _library.SignatureConfidence = RockScan.ConfidenceName(reading.Confidence);
            var text = reading.Confidence == RecognitionConfidence.Missing
                ? ""
                : reading.Value.ToString("0.###", CultureInfo.InvariantCulture);
            if (_signatureText == text)
            {
                NotifyLoadout();
            }
            else
            {
                SignatureText = text;
            }
        }
        finally
        {
            _applyingScan = false;
        }
    }

    public CaptureRectangle? CaptureOf(string deviceName, CaptureTarget target)
    {
        var monitor = _library.FindCapture(deviceName);
        return target == CaptureTarget.Signatures ? monitor?.Signatures : monitor?.Rock;
    }

    private void NotifyLoadout()
    {
        RefreshCrackRows();
        RefreshSignatureWatch();
        if (_suspendSave || SelectedShip is null)
        {
            return;
        }

        LoadoutChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshCrackRows()
    {
        if (_catalog is null)
        {
            _crackRows = [];
            OverlayNote = "The equipment catalog did not load.";
        }
        else
        {
            var loadouts = ComparisonLoadouts();
            _crackRows = loadouts.Count == 0
                ? []
                : LoadoutComparison.Compare(_catalog, ReadRock(), loadouts).Select(OverlayCrackRow.From).ToList();
            OverlayNote = RockNote();
        }

        OnPropertyChanged(nameof(CrackRows));
        OnPropertyChanged(nameof(OverlayNote));
    }

    private void RefreshSignatureWatch()
    {
        if (_catalog is null)
        {
            _signatureHits = [];
        }
        else
        {
            var signature = RockScan.Value(SignatureText, _library.SignatureConfidence);
            var tracked = Minerals.Where(row => row.IsTracked).Select(row => row.Name);
            var result = SignatureWatch.Evaluate(_catalog, signature, tracked, SoundEnabled);
            _signatureHits = result.Hits.Select(OverlaySignatureHit.From).ToList();
            var key = string.Join("|", result.Hits.Where(hit => hit.Blink).Select(hit => hit.Mineral));
            if (!_suspendSave)
            {
                if (_soundArmed && result.PlaySound && key.Length > 0 && key != _soundKey)
                {
                    WatchSound.Play();
                }

                _soundKey = result.PlaySound ? key : "";
                _soundArmed = true;
            }
        }

        OnPropertyChanged(nameof(SignatureHits));
    }

    private List<StoredLoadout> ComparisonLoadouts()
    {
        if (_library.Loadouts.Count > 0)
        {
            return _library.Loadouts;
        }

        if (SelectedShip is null)
        {
            return [];
        }

        var screen = CaptureScreen();
        screen.Name = string.IsNullOrWhiteSpace(LoadoutName) ? "Current" : LoadoutName.Trim();
        return [screen];
    }

    private RockReading ReadRock() =>
        new(
            RockScan.Value(MassText, _library.MassConfidence),
            RockScan.Value(ResistanceText, _library.ResistanceConfidence),
            RockScan.Value(InstabilityText, _library.InstabilityConfidence));

    private string RockNote()
    {
        var rock = ReadRock();
        if (rock.MassKilograms.Confidence == RecognitionConfidence.Uncertain
            || rock.ResistancePercent.Confidence == RecognitionConfidence.Uncertain)
        {
            return "The rock scan is uncertain, so the verdict stays Unknown.";
        }

        return rock.MassKilograms.IsConfirmed && rock.ResistancePercent.IsConfirmed
            ? "Confirmed mass and resistance are used. An uncertain scan stays Unknown."
            : "Mass and resistance have to be confirmed before a verdict.";
    }

    private static string DisplayReading(ObservedValue<double> reading) =>
        reading.Confidence == RecognitionConfidence.Missing
            ? ""
            : reading.Value.ToString("0.###", CultureInfo.CurrentCulture);

    private static bool Near(double? stored, double value) =>
        stored is double current && Math.Abs(current - value) < 0.5;

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

public sealed class LoadoutNameChoice
{
    public LoadoutNameChoice(string name)
    {
        Name = name;
    }

    public string Name { get; }
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
    private IReadOnlyList<SetupStat> _statistics = [];

    public TurretEditor()
    {
        Heads = [];
        foreach (var slot in _slots)
        {
            slot.Changed += () =>
            {
                RefreshStatistics();
                Changed?.Invoke();
            };
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
                RefreshStatistics();
                Changed?.Invoke();
            }
        }
    }

    public StoredTurret Capture() =>
        new()
        {
            HeadId = SelectedHead?.Id,
            LaserOn = IsLaserOn,
            CraftPercent = CraftPercentText,
            Modules = Slots.Select(slot => slot.IsEnabled ? slot.Selected?.Item?.Id : null).ToList()
        };

    public void Apply(StoredTurret saved)
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
                RefreshStatistics();
                Changed?.Invoke();
            }
        }
    }

    public IReadOnlyList<ModuleSlotEditor> Slots => _slots;

    public IReadOnlyList<SetupStat> Statistics => _statistics;

    public bool HasStatistics => _statistics.Count > 0;

    private void RefreshStatistics()
    {
        var head = SelectedHead;
        if (head is null)
        {
            _statistics = [];
        }
        else
        {
            var craft = TryParseCraft(CraftPercentText, out var parsed) ? parsed : 0;
            var summary = LaserStatSummary.Combine(
                head.MinPower,
                head.MaxPower,
                craft,
                head.OptimalRange,
                head.MaximumRange,
                HeadModifiers(head),
                FittedModules());
            _statistics = EquipmentStatText.Laser(summary, head.MaxPower);
        }

        OnPropertyChanged(nameof(Statistics));
        OnPropertyChanged(nameof(HasStatistics));
    }

    private List<LaserStatParts> FittedModules()
    {
        var modules = new List<LaserStatParts>();
        foreach (var slot in _slots)
        {
            if (!slot.IsEnabled)
            {
                continue;
            }

            var module = slot.Selected?.Item;
            if (module is null)
            {
                continue;
            }

            modules.Add(new LaserStatParts(
                module.PowerPercent,
                module.Resistance,
                module.Instability,
                module.GreenZone,
                module.OptimalChargeRate,
                module.Shatter,
                module.OverchargeRate,
                module.InertMaterials,
                module.ExtractPowerPercent,
                0));
        }

        return modules;
    }

    private static LaserStatParts HeadModifiers(MiningHead head) =>
        new(
            0,
            head.Resistance,
            head.Instability,
            head.GreenZone,
            head.OptimalChargeRate,
            0,
            0,
            head.InertMaterials,
            0,
            head.AllChargeRates);

    private static bool TryParseCraft(string text, out double value)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
        {
            return true;
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

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
