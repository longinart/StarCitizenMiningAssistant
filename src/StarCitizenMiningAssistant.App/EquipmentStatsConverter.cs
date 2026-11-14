using System.Globalization;
using System.Windows.Data;
using StarCitizenMiningAssistant.App.ViewModels;

namespace StarCitizenMiningAssistant.App;

public sealed class EquipmentStatsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        EquipmentStatText.Chips(value);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
