using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace StarCitizenMiningAssistant.App;

public partial class CrackDetailWindow : Window
{
    public CrackDetailWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Caption_Drag(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && FindAncestor<System.Windows.Controls.Button>(source) is not null)
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
}
