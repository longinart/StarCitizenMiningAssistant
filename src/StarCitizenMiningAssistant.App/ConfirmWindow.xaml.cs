using System.Windows;

namespace StarCitizenMiningAssistant.App;

public partial class ConfirmWindow : Window
{
    public ConfirmWindow(string message)
        : this("Delete loadout", message, choice: true)
    {
    }

    public ConfirmWindow(string title, string message, bool choice)
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;
        if (!choice)
        {
            NoButton.Visibility = Visibility.Collapsed;
            YesButton.Content = "OK";
        }
    }

    private void Yes_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void No_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
