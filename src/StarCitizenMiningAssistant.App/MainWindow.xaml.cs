using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
        SizeChanged += Window_SizeChanged;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var handle = new WindowInteropHelper(this).Handle;
        var dark = 1;
        var caption = 0x001D1916;
        var text = 0x00EBE8E6;
        DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));
        DwmSetWindowAttribute(handle, 35, ref caption, sizeof(int));
        DwmSetWindowAttribute(handle, 36, ref text, sizeof(int));
        SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, 0x0027);
    }

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
        MarkNav(NavSelectShip);
    }

    private void ShowLoadout_Click(object sender, RoutedEventArgs e)
    {
        if (!_chosen)
        {
            return;
        }

        HideMinerals();
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

    private void MarkNav(Button selected)
    {
        foreach (var button in new[] { NavSelectShip, NavLoadout, NavSignatures })
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

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
