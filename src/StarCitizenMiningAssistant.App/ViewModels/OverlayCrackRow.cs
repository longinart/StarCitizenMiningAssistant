using System.Globalization;
using System.Windows.Media;
using StarCitizenMiningAssistant.Core.Calculation;

namespace StarCitizenMiningAssistant.App.ViewModels;

public sealed class OverlayCrackRow
{
    private static readonly SolidColorBrush Crackable = Freeze(Color.FromRgb(0x3D, 0xDC, 0x7A));
    private static readonly SolidColorBrush Difficult = Freeze(Color.FromRgb(0xE0, 0xB0, 0x3C));
    private static readonly SolidColorBrush Uncrackable = Freeze(Color.FromRgb(0xE1, 0x5B, 0x64));
    private static readonly SolidColorBrush Unknown = Freeze(Color.FromRgb(0xA7, 0xB0, 0xBA));

    private OverlayCrackRow(string name, string verdict, string boosts, string reading, Brush brush)
    {
        Name = name;
        Verdict = verdict;
        Boosts = boosts;
        Reading = reading;
        Brush = brush;
        HasBoosts = boosts.Length > 0;
        HasReading = reading.Length > 0;
    }

    public string Name { get; }

    public string Verdict { get; }

    public string Boosts { get; }

    public string Reading { get; }

    public Brush Brush { get; }

    public bool HasBoosts { get; }

    public bool HasReading { get; }

    public static OverlayCrackRow From(LoadoutCrackRow row)
    {
        var verdict = row.Verdict switch
        {
            CrackVerdict.Crackable => "Crackable",
            CrackVerdict.Difficult => "Difficult",
            CrackVerdict.Uncrackable => "Uncrackable",
            _ => "Unknown"
        };
        var brush = row.Verdict switch
        {
            CrackVerdict.Crackable => Crackable,
            CrackVerdict.Difficult => Difficult,
            CrackVerdict.Uncrackable => Uncrackable,
            _ => Unknown
        };
        return new OverlayCrackRow(row.Name, verdict, row.Boosts, FormatReading(row), brush);
    }

    private static string FormatReading(LoadoutCrackRow row)
    {
        var parts = new List<string>();
        if (row.Instability is double instability)
        {
            parts.Add("Instability " + instability.ToString("0.#", CultureInfo.CurrentCulture));
        }

        if (row.ChargeWindow is double window)
        {
            parts.Add("Window " + window.ToString("0.#", CultureInfo.CurrentCulture));
        }

        return string.Join("   ", parts);
    }

    private static SolidColorBrush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
