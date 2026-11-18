using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using StarCitizenMiningAssistant.Core.Capture;
using StarCitizenMiningAssistant.Core.Recognition;
using StarCitizenMiningAssistant.Infrastructure.Capture;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using StarCitizenMiningAssistant.App.ViewModels;
using StarCitizenMiningAssistant.Core.Equipment;

namespace StarCitizenMiningAssistant.App;

public partial class MainWindow : Window
{
    private static readonly SolidColorBrush NavSelectedBrush = Freeze(Color.FromRgb(0x2A, 0x24, 0x1C));
    private static readonly SolidColorBrush NavTextBrush = Freeze(Color.FromRgb(0xE0, 0x8A, 0x3C));

    private bool _chosen;
    private bool _onPicker = true;
    private Button? _chosenButton;
    private OverlayWindow? _overlay;
    private DispatcherTimer? _watch;
    private bool _watchBusy;
    private DispatcherTimer? _scanLog;
    private bool _scanLogBusy;
    private readonly Dictionary<CaptureTarget, (string Reading, DateTimeOffset At)> _scanLogLast = [];
    private readonly SignatureAim _signatureAim = new();
    private readonly SignatureAim _rockAim = new();

    private static SolidColorBrush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public MainWindow()
        : this(MainWindowViewModel.CreateMissing("The catalog is loaded by application startup."))
    {
    }

    public MainWindow(MainWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
        viewModel.ClickThroughChanged += (_, _) => ApplyOverlayClickThrough();
        viewModel.ScanLogChanged += (_, _) => ApplyScanLog();
        Closed += (_, _) => StopScanLog();
        ApplyScanLog();
        SizeChanged += Window_SizeChanged;
        if (FindRequestedMonitor() is MonitorInfo monitor)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            ShowActivated = false;
            Left = monitor.Left + 40;
            Top = monitor.Top + 40;
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        DarkCaption.Apply(this);
        PlaceOnRequestedMonitor();
    }

    private static int? RequestedMonitor()
    {
        var text = Environment.GetEnvironmentVariable("SCMA_MONITOR");
        return int.TryParse(text, out var number) && number > 0 ? number : null;
    }

    private static MonitorInfo? FindRequestedMonitor()
    {
        if (RequestedMonitor() is not int number)
        {
            return null;
        }

        var monitors = DisplayMonitors.List();
        return monitors.FirstOrDefault(item =>
            item.DeviceName.EndsWith("DISPLAY" + number, StringComparison.OrdinalIgnoreCase))
            ?? (monitors.Count >= number ? monitors[number - 1] : null);
    }

    private void PlaceOnRequestedMonitor()
    {
        if (FindRequestedMonitor() is not MonitorInfo monitor)
        {
            return;
        }

        var width = Math.Min(1420, Math.Max(1340, monitor.Width - 80));
        var height = Math.Min(860, Math.Max(720, monitor.Height - 80));
        var handle = new WindowInteropHelper(this).Handle;
        SetWindowPos(handle, IntPtr.Zero, monitor.Left + 40, monitor.Top + 40, width, height, 0x0014);
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    private void Choice_DropDownOpened(object sender, EventArgs e)
    {
        if (sender is ComboBox box)
        {
            box.Tag = "open";
        }
    }

    private void Choice_DropDownClosed(object sender, EventArgs e)
    {
        if (sender is not ComboBox box || box.Tag as string != "open")
        {
            return;
        }

        box.Tag = null;
        switch (box.DataContext)
        {
            case TurretEditor turret when box.SelectedItem is MiningHead head:
                turret.SelectedHead = head;
                break;
            case ModuleSlotEditor slot when box.SelectedItem is CatalogChoice<MiningModule> choice && !choice.IsHeader:
                slot.Selected = choice;
                break;
            case MainWindowViewModel viewModel when box.SelectedItem is LoadoutNameChoice loadout:
                var before = viewModel.SelectedShip?.Id;
                viewModel.SelectNamedLoadout(loadout.Name);
                if (viewModel.SelectedShip is { } ship && ship.Id != before)
                {
                    Settle(ship.Id);
                }
                break;
            case MainWindowViewModel viewModel when box.SelectedItem is CatalogChoice<MiningGadget> choice:
                viewModel.SelectedGadget = choice;
                break;
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        MarkNav(NavSelectShip);
        if (DataContext is MainWindowViewModel { SelectedShip: { } ship })
        {
            Settle(ship.Id);
        }
    }

    private void Golem_Click(object sender, RoutedEventArgs e) => Choose("drak-golem", GolemButton, ProspectorButton, MoleButton);

    private void Prospector_Click(object sender, RoutedEventArgs e) => Choose("misc-prospector", ProspectorButton, GolemButton, MoleButton);

    private void Mole_Click(object sender, RoutedEventArgs e) => Choose("argo-mole", MoleButton, GolemButton, ProspectorButton);

    private void ChangeShip_Click(object sender, RoutedEventArgs e)
    {
        if (_onPicker)
        {
            return;
        }

        _chosen = false;
        _chosenButton = null;
        _onPicker = true;
        Stage.Opacity = 1;
        Stage.IsHitTestVisible = true;
        HideMinerals();
        HideScan();
        foreach (var button in new[] { GolemButton, ProspectorButton, MoleButton })
        {
            button.IsHitTestVisible = true;
            button.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(280)));
            if (button.RenderTransform is TranslateTransform transform)
            {
                transform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(420)));
                transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(420)));
            }
        }

        Prompt.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(280)));
        Loadout.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(220)));
        Loadout.IsHitTestVisible = false;
        NavLoadout.IsEnabled = false;
        NavSignatures.IsEnabled = false;
        NavOverlay.IsEnabled = false;
        NavScan.IsEnabled = false;
        MarkNav(NavSelectShip);
    }

    private void ShowLoadout_Click(object sender, RoutedEventArgs e)
    {
        if (!_chosen)
        {
            return;
        }

        HideMinerals();
        HideScan();
        Stage.Opacity = 1;
        Stage.IsHitTestVisible = true;
        Prompt.Opacity = 0;
        Loadout.IsHitTestVisible = true;
        Loadout.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(220)));
        if (_chosenButton is not null)
        {
            PlaceChosen(_chosenButton, animate: false);
        }

        _onPicker = false;
        MarkNav(NavLoadout);
    }

    private void ShowSignatures_Click(object sender, RoutedEventArgs e)
    {
        if (!_chosen)
        {
            return;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.CommitLoadout();
        }

        ShowMinerals();
    }

    private void SaveConfiguration_Click(object sender, RoutedEventArgs e) => ShowSignatures_Click(sender, e);

    private void ShowOverlay_Click(object sender, RoutedEventArgs e)
    {
        if (!_chosen)
        {
            return;
        }

        if (_overlay is null)
        {
            _overlay = new OverlayWindow
            {
                DataContext = DataContext,
                Owner = this
            };
            _overlay.Closed += (_, _) =>
            {
                _overlay = null;
                StopWatch();
            };
            _overlay.PlaceFromViewModel();
            _overlay.Show();
            ApplyOverlayClickThrough();
            StartWatch();
            return;
        }

        _overlay.Activate();
        StartWatch();
    }

    private void ApplyOverlayClickThrough()
    {
        if (_overlay is null || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        _overlay.SetClickThrough(viewModel.ClickThrough);
        if (viewModel.ClickThrough && _overlay.IsKeyboardFocusWithin)
        {
            Focus();
        }
    }

    private void StartWatch()
    {
        _watch ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        if (_watch.IsEnabled)
        {
            return;
        }

        _watch.Tick += WatchTick;
        _watch.Start();
    }

    private void StopWatch()
    {
        if (_watch is null)
        {
            return;
        }

        _watch.Stop();
        _watch.Tick -= WatchTick;
    }

    private void ApplyScanLog()
    {
        if (DataContext is MainWindowViewModel { ScanLogEnabled: true })
        {
            StartScanLog();
            return;
        }

        StopScanLog();
    }

    private void StartScanLog()
    {
        _scanLog ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        if (_scanLog.IsEnabled)
        {
            return;
        }

        _scanLog.Tick += ScanLogTick;
        _scanLog.Start();
    }

    private void StopScanLog()
    {
        if (_scanLog is null)
        {
            return;
        }

        _scanLog.Stop();
        _scanLog.Tick -= ScanLogTick;
    }

    private async void ScanLogTick(object? sender, EventArgs e)
    {
        if (_scanLogBusy || DataContext is not MainWindowViewModel viewModel || !viewModel.ScanLogEnabled)
        {
            return;
        }

        _scanLogBusy = true;
        try
        {
            var monitor = WatchMonitor(viewModel);
            await LogTarget(viewModel, monitor, CaptureTarget.Signatures);
            await LogTarget(viewModel, monitor, CaptureTarget.Rock);
        }
        finally
        {
            _scanLogBusy = false;
        }
    }

    private async Task LogTarget(MainWindowViewModel viewModel, MonitorInfo? monitor, CaptureTarget target)
    {
        if (monitor is null)
        {
            return;
        }

        byte[]? captured;
        IReadOnlyList<ObservedValue<string>>? lines;
        var clipped = false;
        if (target == CaptureTarget.Signatures)
        {
            var sought = SeekSignature(monitor, viewModel.SignatureChart);
            captured = sought.Bmp;
            lines = sought.Lines;
        }
        else
        {
            var sought = await SeekRock(monitor);
            captured = sought.Bmp;
            lines = sought.Lines;
        }

        if (captured is null)
        {
            return;
        }

        var record = target == CaptureTarget.Signatures
            ? ScanLogText.Signature(viewModel.Catalog, lines, clipped, viewModel.SignatureChart)
            : ScanLogText.Rock(lines);
        var now = DateTimeOffset.Now;
        DateTimeOffset? previousAt = null;
        string? previous = null;
        if (_scanLogLast.TryGetValue(target, out var prior))
        {
            previous = prior.Reading;
            previousAt = prior.At;
        }

        if (!ScanLogPolicy.ShouldSave(previous, previousAt, record.Reading, now))
        {
            return;
        }

        var label = target == CaptureTarget.Signatures ? "signature" : "rock";
        if (ScanLogFile.Save(label, captured, record.Reading, record.Confidence, record.Detail) is null)
        {
            return;
        }

        _scanLogLast[target] = (record.Reading, now);
    }

    private async void WatchTick(object? sender, EventArgs e)
    {
        if (_watchBusy || _overlay is null || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        _watchBusy = true;
        try
        {
            var monitor = WatchMonitor(viewModel);
            if (_overlay is null)
            {
                return;
            }

            SignatureReadText.Text = await DescribeTarget(viewModel, monitor, CaptureTarget.Signatures);
            if (_overlay is null)
            {
                return;
            }

            RockReadText.Text = await DescribeTarget(viewModel, monitor, CaptureTarget.Rock);
        }
        finally
        {
            _watchBusy = false;
        }
    }

    private MonitorInfo? WatchMonitor(MainWindowViewModel viewModel)
    {
        if (ScanMonitor.SelectedItem is MonitorInfo selected)
        {
            return selected;
        }

        return DisplayMonitors.List().FirstOrDefault(monitor => HasRectangle(viewModel, monitor))
            ?? DisplayMonitors.List().FirstOrDefault();
    }

    private static bool HasRectangle(MainWindowViewModel viewModel, MonitorInfo monitor) =>
        viewModel.CaptureOf(monitor.DeviceName, CaptureTarget.Signatures) is { IsPlaceable: true }
        || viewModel.CaptureOf(monitor.DeviceName, CaptureTarget.Rock) is { IsPlaceable: true };

    private readonly record struct WatchFrame(IReadOnlyList<ObservedValue<string>>? Lines, bool Clipped);

    private async Task<WatchFrame> ReadWatchAsync(
        MainWindowViewModel viewModel,
        MonitorInfo? monitor,
        CaptureTarget target)
    {
        if (monitor is null)
        {
            return new WatchFrame(null, false);
        }

        if (target == CaptureTarget.Signatures)
        {
            var sought = SeekSignature(monitor, viewModel.SignatureChart);
            return new WatchFrame(sought.Lines, false);
        }

        var rockFrame = await SeekRock(monitor);
        return new WatchFrame(rockFrame.Lines, false);
    }

    private async Task<string> DescribeTarget(MainWindowViewModel viewModel, MonitorInfo? monitor, CaptureTarget target)
    {
        if (target == CaptureTarget.Signatures && monitor is null)
        {
            return "Watching. Choose the monitor the game is on.";
        }

        var frame = await ReadWatchAsync(viewModel, monitor, target);
        if (_overlay is null)
        {
            return "";
        }

        if (target == CaptureTarget.Signatures)
        {
            var reading = viewModel.WatchSignature(frame.Lines);
            var names = string.Join(", ", viewModel.SignatureHits.Select(hit => hit.Name));
            return "Watching. " + DescribeHeld("Signature", reading)
                + (names.Length == 0 ? "" : " " + names);
        }

        var lines = frame.Lines;
        var rock = lines is null ? null : RockOcrFields.Read(lines);
        var held = viewModel.WatchRock(rock);
        return "Watching. "
            + DescribeHeld("Mass", new HeldReading(rock?.Mass ?? ObservedValue<double>.Missing(), held.Mass))
            + " " + DescribeHeld("Resistance", new HeldReading(rock?.Resistance ?? ObservedValue<double>.Missing(), held.Resistance))
            + " " + DescribeHeld("Instability", new HeldReading(rock?.Instability ?? ObservedValue<double>.Missing(), held.Instability));
    }

    private static string DescribeHeld(string name, HeldReading reading)
    {
        if (reading.Sample.Confidence == RecognitionConfidence.Missing)
        {
            return reading.Held.Confidence == RecognitionConfidence.Missing
                ? name + " has no number in this frame."
                : name + " kept " + FormatNumber(reading.Held) + ".";
        }

        var state = reading.Held.IsConfirmed ? "confirmed" : "uncertain";
        return name + " " + FormatNumber(reading.Sample) + " " + state + ".";
    }

    private static string FormatNumber(ObservedValue<double> reading) =>
        reading.Confidence == RecognitionConfidence.Missing
            ? ""
            : reading.Value.ToString("0.###", CultureInfo.InvariantCulture);

    private void ShowScan_Click(object sender, RoutedEventArgs e)
    {
        if (!_chosen)
        {
            return;
        }

        ShowScan();
    }

    private void ScanMonitor_Changed(object sender, SelectionChangedEventArgs e) => RefreshScanLabels();

    private void SignatureStill_Click(object sender, RoutedEventArgs e) =>
        ShowStill(CaptureTarget.Signatures, SignatureStill, SignatureStillNote);

    private void RockStill_Click(object sender, RoutedEventArgs e) =>
        ShowStill(CaptureTarget.Rock, RockStill, RockStillNote);

    private async void SignatureRead_Click(object sender, RoutedEventArgs e) =>
        await ReadTextAsync(CaptureTarget.Signatures, SignatureReadText);

    private async void RockRead_Click(object sender, RoutedEventArgs e) =>
        await ReadTextAsync(CaptureTarget.Rock, RockReadText);

    private void SaveLoadout_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SaveNamedLoadout();
        }
    }

    private void DeleteLoadout_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.DeleteNamedLoadout();
        }
    }

    private void MineralName_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: MineralRow row } || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
        {
            if (row.HasSignatures)
            {
                Clipboard.SetText(row.SignatureLine);
                viewModel.NoteCopied(row.Name);
            }

            return;
        }

        row.IsExpanded = !row.IsExpanded;
    }

    private void ShowMinerals()
    {
        HideScan();
        Stage.Opacity = 0;
        Stage.IsHitTestVisible = false;
        Prompt.Opacity = 0;
        Loadout.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(180)));
        Loadout.IsHitTestVisible = false;
        MineralsPage.IsHitTestVisible = true;
        MineralsPage.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(260))
        {
            BeginTime = TimeSpan.FromMilliseconds(120)
        });
        _onPicker = false;
        MarkNav(NavSignatures);
    }

    private void HideMinerals()
    {
        MineralsPage.BeginAnimation(OpacityProperty, null);
        MineralsPage.Opacity = 0;
        MineralsPage.IsHitTestVisible = false;
    }

    private void ShowScan()
    {
        HideMinerals();
        Stage.Opacity = 0;
        Stage.IsHitTestVisible = false;
        Prompt.Opacity = 0;
        Loadout.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(180)));
        Loadout.IsHitTestVisible = false;
        FillScanMonitors();
        ScanPage.IsHitTestVisible = true;
        ScanPage.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(260))
        {
            BeginTime = TimeSpan.FromMilliseconds(120)
        });
        _onPicker = false;
        MarkNav(NavScan);
    }

    private void HideScan()
    {
        ScanPage.BeginAnimation(OpacityProperty, null);
        ScanPage.Opacity = 0;
        ScanPage.IsHitTestVisible = false;
    }

    private void FillScanMonitors()
    {
        var monitors = DisplayMonitors.List();
        var selected = (ScanMonitor.SelectedItem as MonitorInfo)?.DeviceName;
        ScanMonitor.ItemsSource = monitors;
        ScanMonitor.SelectedItem = monitors.FirstOrDefault(monitor =>
            string.Equals(monitor.DeviceName, selected, StringComparison.OrdinalIgnoreCase))
            ?? monitors.FirstOrDefault(monitor => monitor.IsPrimary)
            ?? monitors.FirstOrDefault();
        ScanMonitorNote.Text = monitors.Count == 0
            ? "No monitor was found."
            : "The signature and the scan results use the monitor you pick here.";
        RefreshScanLabels();
    }

    private void RefreshScanLabels()
    {
        SignatureRegionText.Text = "Fixed patch under the radar.";
        RockRegionText.Text = "Fixed patch on the scan results. A miss also checks one step away.";
    }

    private void ShowStill(CaptureTarget target, Image image, TextBlock note)
    {
        if (!TryCapture(target, out var bmp, out var error))
        {
            note.Text = error;
            image.Source = null;
            return;
        }

        image.Source = BitmapFrom(bmp);
        var label = target == CaptureTarget.Signatures ? "signatures" : "rock";
        var path = RegionImage.SaveBmp(label, bmp);
        note.Text = path is null ? "The still image is shown here. It could not be saved." : "Saved " + path;
    }

    private async Task ReadTextAsync(CaptureTarget target, TextBlock output)
    {
        if (target == CaptureTarget.Signatures)
        {
            if (DataContext is not MainWindowViewModel signatureModel || ScanMonitor.SelectedItem is not MonitorInfo gameMonitor)
            {
                output.Text = "Choose the monitor the game is on.";
                return;
            }

            output.Text = "Reading...";
            var sought = SeekSignature(gameMonitor, signatureModel.SignatureChart);
            if (sought.Lines is null)
            {
                output.Text = "No signature number was read.";
                return;
            }

            var lines = sought.Lines;

            var signature = signatureModel.ApplySignatureScan(lines);
            var names = string.Join(", ", signatureModel.SignatureHits.Select(hit => hit.Name));
            output.Text = DescribeSignature(signature)
                + (names.Length == 0 ? "" : Environment.NewLine + names)
                + Environment.NewLine
                + string.Join(Environment.NewLine, lines.Select(FormatLine));
            return;
        }

        if (DataContext is not MainWindowViewModel rockModel || ScanMonitor.SelectedItem is not MonitorInfo rockMonitor)
        {
            output.Text = "Choose a monitor first.";
            return;
        }

        output.Text = "Reading...";
        var rockFrame = await SeekRock(rockMonitor);
        var sample = rockFrame.Lines is null ? null : RockOcrFields.Read(rockFrame.Lines);
        var shown = sample is { HasPanel: true } ? rockModel.WatchRock(sample) : sample;
        output.Text = FormatField("Mass", shown?.Mass ?? ObservedValue<double>.Missing())
            + Environment.NewLine + FormatField("Resistance", shown?.Resistance ?? ObservedValue<double>.Missing())
            + Environment.NewLine + FormatField("Instability", shown?.Instability ?? ObservedValue<double>.Missing());
    }

    private bool TryCapture(CaptureTarget target, out byte[] bmp, out string error)
    {
        bmp = [];
        if (DataContext is not MainWindowViewModel viewModel || ScanMonitor.SelectedItem is not MonitorInfo monitor)
        {
            error = "Choose a monitor first.";
            return false;
        }

        if (target == CaptureTarget.Signatures)
        {
            var patch = CaptureShifted(monitor, _signatureAim.X, _signatureAim.Y)
                ?? CaptureShifted(monitor, 0, 0);
            if (patch is null)
            {
                error = "The signature patch could not be captured.";
                return false;
            }

            bmp = patch;
            error = "";
            return true;
        }

        var shifted = RockPatch.Shifted(monitor.Width, monitor.Height, _rockAim.X, _rockAim.Y)
            ?? RockPatch.Shifted(monitor.Width, monitor.Height, 0, 0);
        var captured = shifted is null
            ? null
            : RegionImage.CaptureBmp(
                monitor.Left + shifted.Value.X,
                monitor.Top + shifted.Value.Y,
                shifted.Value.Width,
                shifted.Value.Height);
        if (captured is null)
        {
            error = "The still image could not be captured.";
            return false;
        }

        bmp = captured;
        error = "";
        return true;
    }

    private (byte[]? Bmp, IReadOnlyList<ObservedValue<string>>? Lines) SeekSignature(MonitorInfo monitor, ScanSignatureChart? chart)
    {
        byte[]? center = null;
        foreach (var (offsetX, offsetY) in _signatureAim.Order(monitor.Height))
        {
            var captured = CaptureShifted(monitor, offsetX, offsetY);
            if (offsetX == 0 && offsetY == 0)
            {
                center = captured;
            }

            if (captured is null)
            {
                continue;
            }

            var lines = ReadPatch(captured, monitor, chart);
            if (lines is null)
            {
                continue;
            }

            _signatureAim.Remember(offsetX, offsetY);
            return (captured, lines);
        }

        return (center ?? CaptureShifted(monitor, 0, 0), null);
    }

    private async Task<(byte[]? Bmp, IReadOnlyList<ObservedValue<string>>? Lines)> SeekRock(MonitorInfo monitor)
    {
        byte[]? center = null;
        foreach (var (offsetX, offsetY) in _rockAim.Order(monitor.Height))
        {
            var shifted = RockPatch.Shifted(monitor.Width, monitor.Height, offsetX, offsetY);
            if (shifted is null)
            {
                continue;
            }

            var captured = RegionImage.CaptureBmp(
                monitor.Left + shifted.Value.X,
                monitor.Top + shifted.Value.Y,
                shifted.Value.Width,
                shifted.Value.Height);
            if (offsetX == 0 && offsetY == 0)
            {
                center = captured;
            }

            if (captured is null)
            {
                continue;
            }

            var words = await new WindowsOcrReader().ReadWordsAsync(captured);
            var fields = RockFrame.Read(captured, words);
            if (!fields.HasPanel)
            {
                continue;
            }

            _rockAim.Remember(offsetX, offsetY);
            return (captured, RockFrame.Lines(fields));
        }

        var home = center ?? CaptureRock(monitor, 0, 0);
        return (home, null);
    }

    private static byte[]? CaptureRock(MonitorInfo monitor, int offsetX, int offsetY)
    {
        var shifted = RockPatch.Shifted(monitor.Width, monitor.Height, offsetX, offsetY);
        return shifted is null
            ? null
            : RegionImage.CaptureBmp(
                monitor.Left + shifted.Value.X,
                monitor.Top + shifted.Value.Y,
                shifted.Value.Width,
                shifted.Value.Height);
    }

    private static byte[]? CaptureShifted(MonitorInfo monitor, int offsetX, int offsetY)
    {
        var patch = ScanPatch.Shifted(monitor.Width, monitor.Height, offsetX, offsetY);
        if (patch is null)
        {
            return null;
        }

        return RegionImage.CaptureBmp(
            monitor.Left + patch.Value.X,
            monitor.Top + patch.Value.Y,
            patch.Value.Width,
            patch.Value.Height);
    }

    private static IReadOnlyList<ObservedValue<string>>? ReadPatch(byte[] bmp, MonitorInfo monitor, ScanSignatureChart? chart)
    {
        if (chart is null)
        {
            return null;
        }

        var hit = HudSignatureReader.ReadBmp(bmp, ScanPatch.DigitHeight(monitor.Height), chart);
        return hit is null ? null : [ObservedValue<string>.Uncertain(hit.Value.Text)];
    }

    private static string FormatLine(ObservedValue<string> line) =>
        line.Value + "   " + ConfidenceWord(line.Confidence);

    private static string DescribeSignature(ObservedValue<double> signature)
    {
        if (signature.Confidence == RecognitionConfidence.Missing)
        {
            return "No signature number was read.";
        }

        var text = signature.Value.ToString("0.###", CultureInfo.InvariantCulture);
        return signature.IsConfirmed
            ? text + " matches one signature, so it is confirmed."
            : text + " stays uncertain.";
    }

    private static string FormatField(string name, ObservedValue<double> value) =>
        value.Confidence == RecognitionConfidence.Missing
            ? name + "   not found"
            : name + "  " + value.Value.ToString("0.###", CultureInfo.CurrentCulture) + "   " + ConfidenceWord(value.Confidence);

    private static string ConfidenceWord(RecognitionConfidence confidence) =>
        confidence == RecognitionConfidence.Confirmed ? "confirmed" : "uncertain";

    private static BitmapImage BitmapFrom(byte[] bmp)
    {
        var image = new BitmapImage();
        using var stream = new MemoryStream(bmp);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private void MarkNav(Button selected)
    {
        foreach (var button in new[] { NavSelectShip, NavLoadout, NavSignatures, NavOverlay, NavScan })
        {
            var active = button == selected;
            button.Background = active ? NavSelectedBrush : Brushes.Transparent;
            if (active)
            {
                button.Foreground = NavTextBrush;
            }
            else
            {
                button.ClearValue(ForegroundProperty);
            }
        }
    }

    private void Choose(string shipId, Button chosen, Button otherA, Button otherB)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectBaseShip(shipId);
        }

        if (_chosen && chosen.Opacity < 1)
        {
            return;
        }

        MoveToTop(chosen, otherA, otherB, animate: true);
    }

    private void Settle(string shipId)
    {
        var chosen = shipId switch
        {
            "drak-golem" => GolemButton,
            "argo-mole" => MoleButton,
            _ => ProspectorButton
        };
        var others = new[] { GolemButton, ProspectorButton, MoleButton }.Where(button => button != chosen).ToArray();
        MoveToTop(chosen, others[0], others[1], animate: false);
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_chosen && _chosenButton is not null)
        {
            PlaceChosen(_chosenButton, animate: false);
        }
    }

    private void MoveToTop(Button chosen, Button otherA, Button otherB, bool animate)
    {
        _chosen = true;
        _chosenButton = chosen;
        chosen.RenderTransformOrigin = new Point(0, 0);
        if (chosen.RenderTransform is not TranslateTransform)
        {
            chosen.RenderTransform = new TranslateTransform();
        }

        PlaceChosen(chosen, animate);

        var fade = new DoubleAnimation(0, animate ? TimeSpan.FromMilliseconds(280) : TimeSpan.Zero);
        otherA.BeginAnimation(OpacityProperty, fade);
        otherB.BeginAnimation(OpacityProperty, fade);
        Prompt.BeginAnimation(OpacityProperty, fade);
        otherA.IsHitTestVisible = false;
        otherB.IsHitTestVisible = false;

        Loadout.IsHitTestVisible = true;
        Loadout.BeginAnimation(OpacityProperty, new DoubleAnimation(1, animate ? TimeSpan.FromMilliseconds(360) : TimeSpan.Zero)
        {
            BeginTime = animate ? TimeSpan.FromMilliseconds(220) : TimeSpan.Zero
        });
        NavLoadout.IsEnabled = true;
        NavSignatures.IsEnabled = true;
        NavOverlay.IsEnabled = true;
        NavScan.IsEnabled = true;
        _onPicker = false;
        MarkNav(NavLoadout);
    }

    private void PlaceChosen(Button chosen, bool animate)
    {
        if (chosen.ActualWidth <= 0 || Root.ActualWidth <= 0)
        {
            return;
        }

        if (chosen.RenderTransform is not TranslateTransform transform)
        {
            transform = new TranslateTransform();
            chosen.RenderTransform = transform;
        }

        var visual = chosen.TranslatePoint(new Point(0, 0), Root);
        var layoutLeft = visual.X - transform.X;
        var layoutTop = visual.Y - transform.Y;
        var dx = (Root.ActualWidth - chosen.ActualWidth) / 2 - layoutLeft;
        var dy = 16 - layoutTop;

        if (animate)
        {
            var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
            var duration = TimeSpan.FromMilliseconds(460);
            transform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(dx, duration) { EasingFunction = ease });
            transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(dy, duration) { EasingFunction = ease });
            return;
        }

        transform.BeginAnimation(TranslateTransform.XProperty, null);
        transform.BeginAnimation(TranslateTransform.YProperty, null);
        transform.X = dx;
        transform.Y = dy;
    }

}
