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

    private OverlayCrackRow(string name, string verdict, string boosts, string powerRatio, string throttle, Brush ratioBrush, Brush brush)
    {
        Name = name;
        Verdict = verdict;
        Boosts = boosts;
        PowerRatioText = powerRatio;
        ThrottleText = throttle;
        PowerRatioBrush = ratioBrush;
        Reading = powerRatio + throttle;
        Brush = brush;
        HasBoosts = boosts.Length > 0;
        HasReading = Reading.Length > 0;
    }

    public string Name { get; }

    public string Verdict { get; }

    public string Boosts { get; }

    public string PowerRatioText { get; }

    public string ThrottleText { get; }

    public Brush PowerRatioBrush { get; }

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
        var ratioText = "";
        var ratioBrush = Unknown;
        if (row.PowerRatio is double ratio)
        {
            var percent = Math.Round(ratio * 100, MidpointRounding.AwayFromZero);
            ratioText = "PR: " + percent.ToString("0", CultureInfo.CurrentCulture) + "%";
            ratioBrush = percent >= 100 ? Crackable : Uncrackable;
        }

        var throttleText = "";
        if (row.StableThrottle is int throttle)
        {
            throttleText = (ratioText.Length > 0 ? "   " : "") + "Throttle: " + throttle.ToString(CultureInfo.CurrentCulture) + "%";
        }

        return new OverlayCrackRow(row.Name, verdict, row.Boosts, ratioText, throttleText, ratioBrush, brush);
    }

    private static SolidColorBrush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
