using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using StarCitizenMiningAssistant.App.ViewModels;
using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.App;

public partial class SignatureOverlayWindow : Window
{
    private bool _ready;
    private bool _passThrough;
    private bool _live = true;
    private string _shown = "";
    private DispatcherTimer? _hide;
    private MainWindowViewModel? _viewModel;

    public SignatureOverlayWindow()
    {
        InitializeComponent();
        Closed += (_, _) =>
        {
            StopHide();
            if (_viewModel is not null)
            {
                _viewModel.PropertyChanged -= OnViewModelChanged;
            }
        };
    }

    public void PlaceFromViewModel()
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.SignatureOverlayLeft is double left && viewModel.SignatureOverlayTop is double top)
        {
            Left = left;
            Top = top;
            return;
        }

        var area = SystemParameters.WorkArea;
        Left = area.Left + 80;
        Top = area.Top + 120;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowClickThrough.Apply(this, _passThrough);
    }

    public void SetLive(bool enabled)
    {
        _live = enabled;
        ApplyChrome();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
            viewModel.PropertyChanged += OnViewModelChanged;
        }

        ApplyChrome();
        _ready = true;
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.SignatureOverlayLine)
            or nameof(MainWindowViewModel.SignatureOverlayPreview)
            or nameof(MainWindowViewModel.SignatureOverlayLocked)
            or nameof(MainWindowViewModel.SignatureOverlayFontSize)
            or nameof(MainWindowViewModel.SignatureOverlayColor)
            or nameof(MainWindowViewModel.SignatureOverlayOpacity)
            or nameof(MainWindowViewModel.ClickThrough)
            or "")
        {
            ApplyChrome();
        }
    }

    private void ApplyChrome()
    {
        if (_viewModel is null)
        {
            return;
        }

        var positioning = _viewModel.SignatureOverlayPreview && !_viewModel.SignatureOverlayLocked;
        Frame.BorderBrush = positioning ? Brushes.LimeGreen : Brushes.Transparent;
        Frame.BorderThickness = positioning ? new Thickness(3) : new Thickness(0);
        Line.FontSize = _viewModel.SignatureOverlayFontSize;
        Line.Foreground = Brush(_viewModel.SignatureOverlayColor);
        Opacity = _viewModel.SignatureOverlayOpacity;
        if (positioning)
        {
            StopHide();
            Line.Text = "2x Stileron";
            Visibility = Visibility.Visible;
        }
        else if (!_live)
        {
            StopHide();
            Visibility = Visibility.Hidden;
        }
        else
        {
            var live = _viewModel.SignatureOverlayLine;
            if (SignatureOverlayNotice.IsNewResult(_shown, live))
            {
                _shown = live.Trim();
                Line.Text = _shown;
                Visibility = Visibility.Visible;
                StartHide();
            }
            else if (_hide is not { IsEnabled: true })
            {
                Visibility = Visibility.Hidden;
            }
        }

        var pass = _viewModel.ClickThrough && _viewModel.SignatureOverlayLocked;
        if (pass == _passThrough)
        {
            return;
        }

        _passThrough = pass;
        WindowClickThrough.Apply(this, pass);
    }

    private void StartHide()
    {
        _hide ??= new DispatcherTimer { Interval = SignatureOverlayNotice.VisibleFor };
        _hide.Stop();
        _hide.Tick -= HideTick;
        _hide.Tick += HideTick;
        _hide.Start();
    }

    private void HideTick(object? sender, EventArgs e)
    {
        StopHide();
        if (_viewModel is { SignatureOverlayPreview: true, SignatureOverlayLocked: false })
        {
            return;
        }

        Visibility = Visibility.Hidden;
    }

    private void StopHide()
    {
        if (_hide is null)
        {
            return;
        }

        _hide.Stop();
        _hide.Tick -= HideTick;
    }

    private void Window_Drag(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel is { SignatureOverlayLocked: false } && e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
            if (_ready)
            {
                _viewModel.RememberSignatureOverlay(Left, Top);
            }
        }
    }

    private static SolidColorBrush Brush(string text)
    {
        var raw = text.StartsWith('#') ? text : "#" + text;
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(raw)!;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or NotSupportedException)
        {
            return Brushes.White;
        }
    }
}
