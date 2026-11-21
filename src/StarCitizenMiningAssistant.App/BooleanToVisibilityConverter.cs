using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StarCitizenMiningAssistant.App;

public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class ScuAmountConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var scu)
            && !double.TryParse(text, NumberStyles.Float, culture, out scu)
            && !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out scu))
        {
            return text;
        }

        return scu < 1
            ? (scu * 100).ToString("0.##", CultureInfo.CurrentCulture) + " cSCU"
            : scu.ToString("0.##", CultureInfo.CurrentCulture) + " SCU";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
