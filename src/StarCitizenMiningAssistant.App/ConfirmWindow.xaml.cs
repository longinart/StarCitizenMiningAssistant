using System.Windows;

namespace StarCitizenMiningAssistant.App;

public partial class ConfirmWindow : Window
{
    public ConfirmWindow(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
    }

    private void Yes_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void No_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
