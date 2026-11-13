using System.Windows;
using StarCitizenMiningAssistant.App.ViewModels;

namespace StarCitizenMiningAssistant.App;

public partial class OverlayWindow : Window
{
    private bool _ready;

    public OverlayWindow()
    {
        InitializeComponent();
    }

    public void PlaceFromViewModel()
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.OverlayWidth is >= 320 && viewModel.OverlayHeight is >= 360)
        {
            Width = viewModel.OverlayWidth.Value;
            Height = viewModel.OverlayHeight.Value;
        }

        if (viewModel.OverlayLeft is double left && viewModel.OverlayTop is double top)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = left;
            Top = top;
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        DarkCaption.Apply(this);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _ready = true;
    }

    private void Window_BoundsChanged(object sender, EventArgs e)
    {
        if (!_ready || WindowState != WindowState.Normal || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.RememberOverlay(Left, Top, ActualWidth, ActualHeight);
    }
}
