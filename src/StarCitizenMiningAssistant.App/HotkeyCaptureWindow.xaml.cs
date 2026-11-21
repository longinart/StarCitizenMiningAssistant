using System.Windows;
using System.Windows.Input;
using StarCitizenMiningAssistant.Core.Input;

namespace StarCitizenMiningAssistant.App;

public partial class HotkeyCaptureWindow : Window
{
    public HotkeyCaptureWindow()
    {
        InitializeComponent();
    }

    public HotkeyChord? Chord { get; private set; }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Focus();
        Keyboard.Focus(this);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            DialogResult = false;
            return;
        }

        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            return;
        }

        var text = HotkeyChord.Format(Modifiers(Keyboard.Modifiers), key.ToString());
        if (HotkeyChord.Parse(text) is not HotkeyChord chord)
        {
            RejectText.Text = "That key is not supported. Letters, digits, function keys, Space, Tab, and Enter are.";
            return;
        }

        Chord = chord;
        RejectText.Text = "";
        Prompt.BeginAnimation(OpacityProperty, null);
        Prompt.Opacity = 1;
        ChordText.Text = chord.Text;
        ApplyButton.IsEnabled = true;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (Chord is null)
        {
            return;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private static int Modifiers(ModifierKeys keys)
    {
        var modifiers = 0;
        if (keys.HasFlag(ModifierKeys.Control))
        {
            modifiers |= HotkeyChord.Control;
        }

        if (keys.HasFlag(ModifierKeys.Alt))
        {
            modifiers |= HotkeyChord.Alt;
        }

        if (keys.HasFlag(ModifierKeys.Shift))
        {
            modifiers |= HotkeyChord.Shift;
        }

        if (keys.HasFlag(ModifierKeys.Windows))
        {
            modifiers |= HotkeyChord.Win;
        }

        return modifiers;
    }
}
