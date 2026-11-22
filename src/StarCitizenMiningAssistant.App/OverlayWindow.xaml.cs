using System.Globalization;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using StarCitizenMiningAssistant.App.ViewModels;

namespace StarCitizenMiningAssistant.App;

public partial class OverlayWindow : Window
{
    private const int HitTestMessage = 0x0084;
    private const int HitClient = 1;
    private const int HitTransparent = -1;

    private static readonly SolidColorBrush SolidBody = Freeze(Color.FromRgb(0x03, 0x07, 0x0B));

    private bool _ready;
    private bool _clickThrough;
    private bool _detailsOpen;
    private int _fadeGeneration;
    private int _armedGeneration;
    private string _crackKey = "";
    private DispatcherTimer? _fade;
    private MainWindowViewModel? _viewModel;

    public OverlayWindow()
    {
        InitializeComponent();
        Closed += (_, _) => StopFade();
    }

    public void SetClickThrough(bool enabled)
    {
        _clickThrough = enabled;
        WindowClickThrough.Apply(this, false);
    }

    private static SolidColorBrush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public void PlaceFromViewModel()
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.OverlayLeft is double left && viewModel.OverlayTop is double top)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = left;
            Top = top;
        }

        ApplyFixedSize();
    }

    private const double BaseWidth = 540;
    private const double BaseHeight = 1100;

    private void ApplyFixedSize()
    {
        var scale = DataContext is MainWindowViewModel viewModel ? viewModel.RockOverlayScale : 1;
        Width = BaseWidth * scale;
        Height = BaseHeight * scale;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        DarkCaption.Apply(this);
        WindowClickThrough.Apply(this, false);
        if (PresentationSource.FromVisual(this) is HwndSource source)
        {
            source.AddHook(RouteClick);
        }
    }

    private IntPtr RouteClick(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != HitTestMessage || !_clickThrough)
        {
            return IntPtr.Zero;
        }

        var packed = lParam.ToInt32();
        var screen = new Point((short)(packed & 0xFFFF), (short)((packed >> 16) & 0xFFFF));
        var point = PointFromScreen(screen);
        handled = true;
        return new IntPtr(HitsChrome(point) ? HitClient : HitTransparent);
    }

    private bool HitsChrome(Point point)
    {
        if (point.X < 0 || point.Y < 0 || point.X > ActualWidth || point.Y > ActualHeight)
        {
            return false;
        }

        if (point.Y <= TitleBar.ActualHeight + DetailsChrome.ActualHeight + CompositionChrome.ActualHeight)
        {
            return true;
        }

        return false;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
            viewModel.PropertyChanged += OnViewModelChanged;
            viewModel.Composition.Rows.CollectionChanged += (_, _) =>
            {
                if (viewModel.Composition.Rows.Count > 0 && !_compositionOpen)
                {
                    ShowComposition();
                }
            };
        }

        FitShade();
        ClipTitle();
        SyncFade(force: true);
        _ready = true;
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.CrackRows) or "")
        {
            SyncFade(force: false);
        }

        if (e.PropertyName is nameof(MainWindowViewModel.RockOverlayScale) or "")
        {
            ApplyFixedSize();
        }
    }

    private void ToggleLock_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ToggleCompositionLock();
        }
    }

    private void Rescan_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.RescanComposition();
        }
    }

    private void Window_BoundsChanged(object sender, EventArgs e)
    {
        FitShade();
        ClipTitle();
        if (!_ready || WindowState != WindowState.Normal || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.RememberOverlay(Left, Top, ActualWidth, ActualHeight);
    }

    private void TitleButton_Up(object sender, MouseButtonEventArgs e) => e.Handled = true;

    private void Caption_Drag(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || FindAncestor<System.Windows.Controls.Button>(source) is not null)
        {
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private static T? FindAncestor<T>(DependencyObject? node) where T : DependencyObject
    {
        while (node is not null)
        {
            if (node is T match)
            {
                return match;
            }

            node = VisualTreeHelper.GetParent(node);
        }

        return null;
    }

    private void Close_Click(object sender, RoutedEventArgs e) =>
        Dispatcher.BeginInvoke(new Action(Close));

    private void ToggleDetails_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _detailsOpen = !_detailsOpen;
        Details.Visibility = _detailsOpen ? Visibility.Visible : Visibility.Collapsed;
        Chevron.Data = Geometry.Parse(_detailsOpen ? "M0,8 L8,0 L16,8 Z" : "M0,0 L8,8 L16,0 Z");
    }

    private bool _compositionOpen;

    private void ToggleComposition_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _compositionOpen = !_compositionOpen;
        CompositionPanel.Visibility = _compositionOpen ? Visibility.Visible : Visibility.Collapsed;
        CompositionChevron.Data = Geometry.Parse(_compositionOpen ? "M0,8 L8,0 L16,8 Z" : "M0,0 L8,8 L16,0 Z");
    }

    private void ShowComposition()
    {
        _compositionOpen = true;
        CompositionPanel.Visibility = Visibility.Visible;
        CompositionChevron.Data = Geometry.Parse("M0,8 L8,0 L16,8 Z");
    }

    private void Calculate_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.CalculateRock();
        }
    }

    private void AddMineral_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.Composition.AddRow();
        }
    }

    private void RemoveMineral_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.Composition.RemoveLast();
        }
    }

    private double _nameColumnWidth;

    private void MineralName_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox box || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var nameWidth = NameColumnWidth(box, viewModel);
        box.Width = nameWidth;
        box.HorizontalAlignment = HorizontalAlignment.Left;
        if (box.Parent is Grid grid && grid.ColumnDefinitions.Count == 3)
        {
            grid.ColumnDefinitions[0].Width = new GridLength(nameWidth);
            grid.ColumnDefinitions[1].Width = new GridLength(96);
            var amountFont = box.TryFindResource("Lapd.FontMono") as FontFamily;
            grid.ColumnDefinitions[2].Width = new GridLength(Measure(box, "100,00 cSCU", 19, amountFont) + 14);
        }

    }

    private double NameColumnWidth(ComboBox box, MainWindowViewModel viewModel)
    {
        if (_nameColumnWidth > 0)
        {
            return _nameColumnWidth;
        }

        var longest = 0.0;
        foreach (var name in viewModel.Composition.Choices)
        {
            var text = Measure(box, name, 18);
            if (text > longest)
            {
                longest = text;
            }
        }

        _nameColumnWidth = Math.Ceiling((longest + 36) * 1.2);
        return _nameColumnWidth;
    }

    private static double Measure(Visual visual, string text, double size, FontFamily? family = null)
    {
        var dpi = VisualTreeHelper.GetDpi(visual).PixelsPerDip;
        var typeface = new Typeface(family ?? new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, size, Brushes.White, dpi).Width;
    }

    private void FitShade()
    {
        var height = BodyShade.ActualHeight;
        var width = BodyShade.ActualWidth;
        if (height < 1 || width < 1)
        {
            return;
        }

        BodyShade.OpacityMask = null;
        var fade = Math.Min(120, height * 0.35);
        if (fade < 16)
        {
            BodyShade.Background = SolidBody;
            return;
        }

        var hold = (height - fade) / height;
        var fill = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        fill.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x03, 0x07, 0x0B), 0));
        fill.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x03, 0x07, 0x0B), hold));
        fill.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x03, 0x07, 0x0B), 1));
        BodyShade.Background = fill;
    }

    private void ClipTitle()
    {
        if (TitleBar.ActualWidth < 1 || TitleBar.ActualHeight < 1)
        {
            return;
        }

        TitleBar.Clip = TopRounded(TitleBar.ActualWidth, TitleBar.ActualHeight, 16);
    }

    private static Geometry TopRounded(double width, double height, double radius)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(0, height), true, true);
            context.LineTo(new Point(0, radius), true, false);
            context.ArcTo(new Point(radius, 0), new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, false);
            context.LineTo(new Point(width - radius, 0), true, false);
            context.ArcTo(new Point(width, radius), new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, false);
            context.LineTo(new Point(width, height), true, false);
        }

        geometry.Freeze();
        return geometry;
    }

    private void SyncFade(bool force)
    {
        var key = _viewModel is null
            ? ""
            : string.Join("\n", _viewModel.CrackRows.Select(row => row.Name + "|" + row.Verdict + "|" + row.Boosts + "|" + row.Reading));
        if (!force && key == _crackKey)
        {
            return;
        }

        _crackKey = key;
        _fade?.Stop();
        _fadeGeneration++;
        CrackHost.BeginAnimation(OpacityProperty, null);
        if (key.Length == 0)
        {
            CrackHost.Opacity = 0;
            CrackHost.Visibility = Visibility.Collapsed;
            return;
        }

        CrackHost.Visibility = Visibility.Visible;
        CrackHost.Opacity = 1;
        _armedGeneration = _fadeGeneration;
        _fade ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _fade.Tick -= FadeTick;
        _fade.Tick += FadeTick;
        _fade.Start();
    }

    private void FadeTick(object? sender, EventArgs e)
    {
        _fade?.Stop();
        var generation = _armedGeneration;
        if (generation != _fadeGeneration)
        {
            return;
        }

        var animation = new DoubleAnimation(0, TimeSpan.FromMilliseconds(800));
        animation.Completed += (_, _) =>
        {
            if (generation == _fadeGeneration)
            {
                CrackHost.Visibility = Visibility.Collapsed;
            }
        };
        CrackHost.BeginAnimation(OpacityProperty, animation);
    }

    private void StopFade()
    {
        _fade?.Stop();
        if (_fade is not null)
        {
            _fade.Tick -= FadeTick;
        }

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelChanged;
        }
    }
}
