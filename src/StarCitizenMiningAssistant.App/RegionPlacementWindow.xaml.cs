using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using StarCitizenMiningAssistant.Core.Capture;
using StarCitizenMiningAssistant.Infrastructure.Capture;

namespace StarCitizenMiningAssistant.App;

public partial class RegionPlacementWindow : Window
{
    private readonly MonitorInfo _monitor;
    private Point _start;
    private bool _dragging;
    private CaptureRectangle? _chosen;

    private RegionPlacementWindow(MonitorInfo monitor, string hint)
    {
        _monitor = monitor;
        InitializeComponent();
        Hint.Text = hint + " Enter keeps it. Escape cancels.";
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = monitor.Left;
        Top = monitor.Top;
        Width = monitor.Width;
        Height = monitor.Height;
    }

    public static CaptureRectangle? Ask(Window owner, MonitorInfo monitor, string hint)
    {
        var window = new RegionPlacementWindow(monitor, hint) { Owner = owner };
        return window.ShowDialog() == true ? window._chosen : null;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var handle = new WindowInteropHelper(this).Handle;
        SetWindowPos(handle, new IntPtr(-1), _monitor.Left, _monitor.Top, _monitor.Width, _monitor.Height, 0x0040);
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragging = true;
        _start = e.GetPosition(this);
        Hint.Visibility = Visibility.Collapsed;
        CaptureMouse();
        MoveSelection(_start);
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dragging)
        {
            MoveSelection(e.GetPosition(this));
        }
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        ReleaseMouseCapture();
        _chosen = ToRectangle(e.GetPosition(this));
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && _chosen is { IsPlaceable: true })
        {
            DialogResult = true;
            e.Handled = true;
        }
    }

    private void MoveSelection(Point current)
    {
        var left = Math.Min(_start.X, current.X);
        var top = Math.Min(_start.Y, current.Y);
        var width = Math.Abs(current.X - _start.X);
        var height = Math.Abs(current.Y - _start.Y);
        Canvas.SetLeft(Selection, left);
        Canvas.SetTop(Selection, top);
        Selection.Width = width;
        Selection.Height = height;
        Selection.Visibility = Visibility.Visible;
    }

    private CaptureRectangle? ToRectangle(Point current)
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        var left = Math.Min(_start.X, current.X) * dpi.DpiScaleX;
        var top = Math.Min(_start.Y, current.Y) * dpi.DpiScaleY;
        var width = Math.Abs(current.X - _start.X) * dpi.DpiScaleX;
        var height = Math.Abs(current.Y - _start.Y) * dpi.DpiScaleY;
        var rectangle = new CaptureRectangle { Left = left, Top = top, Width = width, Height = height };
        return rectangle.IsPlaceable ? rectangle : null;
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
