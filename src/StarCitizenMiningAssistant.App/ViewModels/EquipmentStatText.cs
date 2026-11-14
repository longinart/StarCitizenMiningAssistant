using System.Globalization;
using System.Windows.Media;
using StarCitizenMiningAssistant.Core.Calculation;
using StarCitizenMiningAssistant.Core.Equipment;

namespace StarCitizenMiningAssistant.App.ViewModels;

public sealed class SetupStat
{
    public SetupStat(string label, string value, Brush brush)
    {
        Label = label;
        Value = value;
        Brush = brush;
    }

    public string Label { get; }

    public string Value { get; }

    public Brush Brush { get; }
}

public sealed class StatChip
{
    public StatChip(string text, Brush brush)
    {
        Text = text;
        Brush = brush;
    }

    public string Text { get; }

    public Brush Brush { get; }
}

public static class EquipmentStatText
{
    private static readonly SolidColorBrush Plus = Freeze(Color.FromRgb(0x3D, 0xDC, 0x7A));
    private static readonly SolidColorBrush Minus = Freeze(Color.FromRgb(0xE1, 0x5B, 0x64));
    private static readonly SolidColorBrush Neutral = Freeze(Color.FromRgb(0xA7, 0xB0, 0xBA));
    private static readonly SolidColorBrush Active = Freeze(Color.FromRgb(0xE0, 0x8A, 0x3C));

    public static IReadOnlyList<SetupStat> Laser(LaserStatSummary summary, double basePower)
    {
        var powerChange = basePower == 0 ? 0 : summary.PowerWatts - basePower;
        return
        [
            Line("Power", Watts(summary.PowerWatts), powerChange),
            new SetupStat("Minimum power", Watts(summary.MinPowerWatts), Neutral),
            Penalty("Resistance", summary.ResistancePercent),
            Penalty("Instability", summary.InstabilityPercent),
            Line("Optimal charge rate", Percent(summary.ChargeRatePercent), summary.ChargeRatePercent),
            Line("All charge rates", Percent(summary.AllChargeRatesPercent), summary.AllChargeRatesPercent),
            Line("Charge window size", Percent(summary.WindowSizePercent), summary.WindowSizePercent),
            Line("Overcharge rate", Percent(summary.OverchargePercent), summary.OverchargePercent),
            Line("Shatter", Percent(summary.ShatterPercent), summary.ShatterPercent),
            Line("Inert materials", Percent(summary.InertPercent), summary.InertPercent),
            Line("Extract power", Percent(summary.ExtractPowerPercent), summary.ExtractPowerPercent),
            new SetupStat("Optimal range", Meters(summary.OptimalRange), Neutral),
            new SetupStat("Maximum range", Meters(summary.MaximumRange), Neutral)
        ];
    }

    public static IReadOnlyList<SetupStat> Gadget(MiningGadget gadget)
    {
        return
        [
            Line("Power", Percent(gadget.PowerPercent), gadget.PowerPercent),
            Penalty("Resistance", gadget.Resistance),
            Penalty("Instability", gadget.Instability),
            Line("Optimal charge rate", Percent(gadget.OptimalChargeRate), gadget.OptimalChargeRate),
            Line("Charge window size", Percent(gadget.GreenZone), gadget.GreenZone),
            Line("Shatter", Percent(gadget.Shatter), gadget.Shatter),
            Line("Clustering", Percent(gadget.Clustering), gadget.Clustering)
        ];
    }

    public static IReadOnlyList<StatChip> Chips(object? value)
    {
        return value switch
        {
            MiningHead head => HeadChips(head),
            CatalogChoice<MiningModule> module => ModuleChips(module),
            CatalogChoice<MiningGadget> gadget => GadgetChips(gadget),
            _ => []
        };
    }

    private static IReadOnlyList<StatChip> HeadChips(MiningHead head)
    {
        var chips = new List<StatChip>
        {
            new(WattRange(head.MinPower, head.MaxPower), Neutral)
        };
        AddPercent(chips, head.Resistance, "resistance", lowerIsBetter: true);
        AddPercent(chips, head.Instability, "instability", lowerIsBetter: true);
        AddPercent(chips, head.OptimalChargeRate, "charge");
        AddPercent(chips, head.AllChargeRates, "all charge");
        AddPercent(chips, head.GreenZone, "window");
        AddPercent(chips, head.InertMaterials, "inert");
        AddMeters(chips, head.OptimalRange, "optimal");
        AddMeters(chips, head.MaximumRange, "max");
        return chips;
    }

    private static IReadOnlyList<StatChip> ModuleChips(CatalogChoice<MiningModule> choice)
    {
        var module = choice.Item;
        if (choice.IsHeader || module is null)
        {
            return [];
        }

        var chips = new List<StatChip>();
        if (module.Active)
        {
            chips.Add(new StatChip("Active", Active));
            if (module.Charges > 0)
            {
                chips.Add(new StatChip(module.Charges + " charges", Neutral));
            }

            if (module.DurationSeconds is > 0)
            {
                chips.Add(new StatChip(Seconds(module.DurationSeconds.Value), Neutral));
            }
        }

        AddPercent(chips, module.PowerPercent, "power");
        AddPercent(chips, module.Resistance, "resistance", lowerIsBetter: true);
        AddPercent(chips, module.Instability, "instability", lowerIsBetter: true);
        AddPercent(chips, module.OptimalChargeRate, "charge");
        AddPercent(chips, module.GreenZone, "window");
        AddPercent(chips, module.OverchargeRate, "overcharge");
        AddPercent(chips, module.Shatter, "shatter");
        AddPercent(chips, module.InertMaterials, "inert");
        AddPercent(chips, module.ExtractPowerPercent, "extract");
        return chips;
    }

    private static IReadOnlyList<StatChip> GadgetChips(CatalogChoice<MiningGadget> choice)
    {
        var gadget = choice.Item;
        if (gadget is null)
        {
            return [];
        }

        var chips = new List<StatChip>();
        AddPercent(chips, gadget.PowerPercent, "power");
        AddPercent(chips, gadget.Resistance, "resistance", lowerIsBetter: true);
        AddPercent(chips, gadget.Instability, "instability", lowerIsBetter: true);
        AddPercent(chips, gadget.OptimalChargeRate, "charge");
        AddPercent(chips, gadget.GreenZone, "window");
        AddPercent(chips, gadget.Shatter, "shatter");
        AddPercent(chips, gadget.Clustering, "clustering");
        return chips;
    }

    private static void AddPercent(List<StatChip> chips, double value, string name, bool lowerIsBetter = false)
    {
        if (value == 0)
        {
            return;
        }

        chips.Add(new StatChip(Percent(value) + " " + name, Tone(value, lowerIsBetter)));
    }

    private static void AddMeters(List<StatChip> chips, double meters, string name)
    {
        if (meters == 0)
        {
            return;
        }

        chips.Add(new StatChip(Meters(meters) + " " + name, Neutral));
    }

    private static SetupStat Line(string label, string value, double signed) =>
        new(label, value, Tone(signed, lowerIsBetter: false));

    private static SetupStat Penalty(string label, double signed) =>
        new(label, Percent(signed), Tone(signed, lowerIsBetter: true));

    private static Brush Tone(double value, bool lowerIsBetter)
    {
        if (value > 0.0000001)
        {
            return lowerIsBetter ? Minus : Plus;
        }

        if (value < -0.0000001)
        {
            return lowerIsBetter ? Plus : Minus;
        }

        return Neutral;
    }

    private static string Percent(double value)
    {
        var rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero);
        var text = rounded.ToString("0.#", CultureInfo.CurrentCulture);
        return rounded > 0 ? "+" + text + "%" : text + "%";
    }

    private static string Watts(double value) => Number(value) + " W";

    private static string WattRange(double min, double max) => Number(min) + "–" + Number(max) + " W";

    private static string Number(double value)
    {
        var rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        return rounded.ToString("0.##", CultureInfo.CurrentCulture);
    }

    private static string Meters(double value)
    {
        var rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero);
        return rounded.ToString("0.#", CultureInfo.CurrentCulture) + " m";
    }

    private static string Seconds(double value)
    {
        var rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero);
        return rounded.ToString("0.#", CultureInfo.CurrentCulture) + " s";
    }

    private static SolidColorBrush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
