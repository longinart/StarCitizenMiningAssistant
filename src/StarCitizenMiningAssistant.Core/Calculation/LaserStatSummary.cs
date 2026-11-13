namespace StarCitizenMiningAssistant.Core.Calculation;

/// <summary>
/// Percent modifiers on one laser or module. A value of -30 is the factor 0.70.
/// Extract power is not laser power. All charge rates is not optimal charge rate.
/// </summary>
public readonly record struct LaserStatParts(
    double PowerPercent,
    double ResistancePercent,
    double InstabilityPercent,
    double GreenZonePercent,
    double ChargeRatePercent,
    double ShatterPercent,
    double OverchargePercent,
    double InertPercent,
    double ExtractPowerPercent,
    double AllChargeRatesPercent)
{
    public static LaserStatParts None { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}

/// <summary>
/// One laser after its craft percent and fitted modules. This is the loadout
/// display. It does not decide whether a rock cracks. Percent fields use the
/// same product as resistance: each piece contributes <c>1 + percent / 100</c>.
/// </summary>
public readonly record struct LaserStatSummary(
    double PowerWatts,
    double MinPowerWatts,
    double OptimalRange,
    double MaximumRange,
    double ResistancePercent,
    double InstabilityPercent,
    double ChargeRatePercent,
    double AllChargeRatesPercent,
    double WindowSizePercent,
    double OverchargePercent,
    double ShatterPercent,
    double InertPercent,
    double ExtractPowerPercent)
{
    public static LaserStatSummary Combine(
        double minPower,
        double maxPower,
        double craftPercent,
        double optimalRange,
        double maximumRange,
        LaserStatParts head,
        IReadOnlyList<LaserStatParts> modules)
    {
        var power = 1 + craftPercent / 100;
        var resistance = Factor(head.ResistancePercent);
        var instability = Factor(head.InstabilityPercent);
        var charge = Factor(head.ChargeRatePercent);
        var allCharge = Factor(head.AllChargeRatesPercent);
        var window = Factor(head.GreenZonePercent);
        var overcharge = Factor(head.OverchargePercent);
        var shatter = Factor(head.ShatterPercent);
        var inert = Factor(head.InertPercent);
        var extract = Factor(head.ExtractPowerPercent);

        foreach (var module in modules)
        {
            power *= Factor(module.PowerPercent);
            resistance *= Factor(module.ResistancePercent);
            instability *= Factor(module.InstabilityPercent);
            charge *= Factor(module.ChargeRatePercent);
            allCharge *= Factor(module.AllChargeRatesPercent);
            window *= Factor(module.GreenZonePercent);
            overcharge *= Factor(module.OverchargePercent);
            shatter *= Factor(module.ShatterPercent);
            inert *= Factor(module.InertPercent);
            extract *= Factor(module.ExtractPowerPercent);
        }

        return new(
            maxPower * power,
            minPower,
            optimalRange,
            maximumRange,
            ToPercent(resistance),
            ToPercent(instability),
            ToPercent(charge),
            ToPercent(allCharge),
            ToPercent(window),
            ToPercent(overcharge),
            ToPercent(shatter),
            ToPercent(inert),
            ToPercent(extract));
    }

    private static double Factor(double percent) => 1 + percent / 100;

    private static double ToPercent(double factor) => (factor - 1) * 100;
}
