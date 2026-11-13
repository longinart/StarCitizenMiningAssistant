namespace StarCitizenMiningAssistant.Core.Calculation;

/// <summary>
/// SCMiner checker, reimplemented from the public calculator's expressions.
/// Resistance lowers delivered power. Required power stays 0.36 times mass.
/// </summary>
public static class CrackabilityCalculator
{
    public const string FormulaVersion = "scminer-checker-2026-11-12";

    public const double PowerPerKilogram = 0.36;

    public static CrackAssessment Assess(
        RockReading rock,
        IReadOnlyList<LaserContribution> lasers,
        ModifierSet? gadget = null,
        ModifierSet? consumable = null)
    {
        ArgumentNullException.ThrowIfNull(lasers);

        if (!rock.MassKilograms.IsConfirmed || !rock.ResistancePercent.IsConfirmed)
        {
            return Unknown("Mass and resistance have to be confirmed readings.");
        }

        var mass = rock.MassKilograms.Value;
        var resistance = rock.ResistancePercent.Value;
        if (mass < 0 || resistance < 0 || resistance > 100)
        {
            return Unknown("Mass has to be zero or more, and resistance has to be from 0 to 100.");
        }

        var required = PowerPerKilogram * mass;
        if (lasers.Count == 0)
        {
            return new CrackAssessment(
                FormulaVersion,
                CrackBand.NeedsMoreLasers,
                false,
                required,
                0,
                0,
                resistance,
                InstabilityOrNull(rock, 1),
                18,
                null,
                0,
                null,
                "No active mining laser is included.");
        }

        var fleet = Combine(lasers, gadget, consumable);
        var effectiveResistance = Clamp(resistance * fleet.ResistanceFactor, 0, 98);
        var delivered = fleet.Power * (1 - effectiveResistance / 100);
        var ratio = delivered / Math.Max(1, required);
        var canCrack = delivered >= required;
        var band = BandFor(canCrack, ratio);
        int? throttle = canCrack && delivered > 0
            ? (int)Clamp(Round(1.08 * required / delivered * 100), 5, 100)
            : null;
        var crackTime = RoundTenths(14 / Math.Max(0.3, fleet.ChargeRateFactor));

        return new CrackAssessment(
            FormulaVersion,
            band,
            canCrack,
            required,
            delivered,
            ratio,
            RoundTenths(effectiveResistance),
            InstabilityOrNull(rock, fleet.InstabilityFactor),
            RoundTenths(Clamp(18 * fleet.GreenZoneFactor, 4, 65)),
            throttle,
            Round(delivered / PowerPerKilogram),
            crackTime,
            ReasonFor(band, ratio));
    }

    public static string DescribeAssistance(
        CrackAssessment bare,
        CrackAssessment? gadgetOnly,
        CrackAssessment? consumableOnly,
        CrackAssessment? both,
        string? gadgetName,
        string? consumableName)
    {
        ArgumentNullException.ThrowIfNull(bare);

        if (bare.Band == CrackBand.Unknown)
        {
            return bare.Reason;
        }

        if (bare.CanCrack == true)
        {
            return "The lasers can crack this rock without a gadget or an active module.";
        }

        var gadgetCracks = gadgetOnly?.CanCrack == true;
        var consumableCracks = consumableOnly?.CanCrack == true;
        var bothCrack = both?.CanCrack == true;
        var gadget = string.IsNullOrWhiteSpace(gadgetName) ? "The gadget" : gadgetName;
        var consumable = string.IsNullOrWhiteSpace(consumableName) ? "The active module" : consumableName;

        if (gadgetOnly is null && consumableOnly is null)
        {
            return NearThreshold(bare)
                ? "The lasers reach " + Percent(bare) + "% of the required power. A gadget or an active module may close this gap."
                : "These lasers cannot crack this rock. Choose a gadget or an active module to see whether either is enough.";
        }

        if (gadgetCracks && consumableCracks)
        {
            return $"{gadget} alone is enough, and {consumable} alone is enough. You do not need both.";
        }

        if (gadgetCracks)
        {
            return $"{gadget} alone is enough.";
        }

        if (consumableCracks)
        {
            return $"{consumable} alone is enough.";
        }

        if (bothCrack)
        {
            return $"You need {gadget} and {consumable} together.";
        }

        if (gadgetOnly is not null && consumableOnly is not null)
        {
            var together = both ?? consumableOnly;
            return NearThreshold(together)
                ? $"{gadget} and {consumable} together reach {Percent(together)}% of the required power. This is the difficult band: close, and still short of a crack."
                : $"{gadget} and {consumable} together are still not enough. This rock needs more lasers.";
        }

        if (gadgetOnly is not null)
        {
            return NearThreshold(gadgetOnly)
                ? $"{gadget} reaches {Percent(gadgetOnly)}% of the required power. This is the difficult band: close, and still short of a crack."
                : $"{gadget} is not enough. This rock needs more lasers.";
        }

        return NearThreshold(consumableOnly)
            ? $"{consumable} reaches {Percent(consumableOnly!)}% of the required power. This is the difficult band: close, and still short of a crack."
            : $"{consumable} is not enough. This rock needs more lasers.";
    }

    private static bool NearThreshold(CrackAssessment? assessment) =>
        assessment?.CanCrack == false && assessment.PowerRatio is >= 0.7;

    private static int Percent(CrackAssessment assessment) =>
        Round((assessment.PowerRatio ?? 0) * 100);

    private static FleetFactors Combine(
        IReadOnlyList<LaserContribution> lasers,
        ModifierSet? gadget,
        ModifierSet? consumable)
    {
        double power = 0;
        double resistance = 0;
        double instability = 0;
        double greenZone = 0;
        double chargeRate = 0;
        double shatter = 0;

        foreach (var laser in lasers)
        {
            var powerFactor = 1 + laser.CraftPowerPercent / 100;
            var resistanceFactor = laser.Head.ResistanceFactor;
            var instabilityFactor = laser.Head.InstabilityFactor;
            var greenFactor = laser.Head.GreenZoneFactor;
            var chargeFactor = laser.Head.ChargeRateFactor;
            var shatterFactor = laser.Head.ShatterFactor;

            foreach (var module in laser.PassiveModules)
            {
                powerFactor *= module.PowerFactor;
                resistanceFactor *= module.ResistanceFactor;
                instabilityFactor *= module.InstabilityFactor;
                greenFactor *= module.GreenZoneFactor;
                chargeFactor *= module.ChargeRateFactor;
                shatterFactor *= module.ShatterFactor;
            }

            power += laser.MaxPower * powerFactor;
            resistance += resistanceFactor;
            instability += instabilityFactor;
            greenZone += greenFactor;
            chargeRate += chargeFactor;
            shatter += shatterFactor;
        }

        var count = lasers.Count;
        var resistanceAverage = resistance / count;
        var instabilityAverage = instability / count;
        var greenAverage = greenZone / count;
        var chargeAverage = chargeRate / count;
        var shatterAverage = shatter / count;

        ApplyFleet(gadget);
        ApplyFleet(consumable);

        return new FleetFactors(power, resistanceAverage, instabilityAverage, greenAverage, chargeAverage, shatterAverage);

        void ApplyFleet(ModifierSet? extra)
        {
            if (extra is null)
            {
                return;
            }

            power *= extra.Value.PowerFactor;
            resistanceAverage *= extra.Value.ResistanceFactor;
            instabilityAverage *= extra.Value.InstabilityFactor;
            greenAverage *= extra.Value.GreenZoneFactor;
            chargeAverage *= extra.Value.ChargeRateFactor;
            shatterAverage *= extra.Value.ShatterFactor;
        }
    }

    private static double? InstabilityOrNull(RockReading rock, double factor)
    {
        if (!rock.Instability.IsConfirmed)
        {
            return null;
        }

        return RoundTenths(Clamp(rock.Instability.Value * factor, 0, 99));
    }

    private static CrackBand BandFor(bool canCrack, double ratio)
    {
        if (canCrack && ratio >= 1.6)
        {
            return CrackBand.Trivial;
        }

        if (canCrack && ratio >= 1.15)
        {
            return CrackBand.Comfortable;
        }

        if (canCrack)
        {
            return CrackBand.Borderline;
        }

        if (ratio >= 0.7)
        {
            return CrackBand.NeedsGadgetOrConsumable;
        }

        return CrackBand.NeedsMoreLasers;
    }

    private static string ReasonFor(CrackBand band, double ratio)
    {
        var percent = Round(ratio * 100);
        return band switch
        {
            CrackBand.Trivial => $"Power ratio {percent}%. The lasers crack this rock with a wide margin.",
            CrackBand.Comfortable => $"Power ratio {percent}%. The lasers crack this rock.",
            CrackBand.Borderline => $"Power ratio {percent}%. The lasers can crack this rock near full throttle.",
            CrackBand.NeedsGadgetOrConsumable => $"Power ratio {percent}%. The lasers are short. A gadget or an active module may close the gap.",
            _ => $"Power ratio {percent}%. These lasers are well short of the rock."
        };
    }

    private static CrackAssessment Unknown(string reason) =>
        new(FormulaVersion, CrackBand.Unknown, null, null, null, null, null, null, null, null, null, null, reason);

    private static double Clamp(double value, double minimum, double maximum) =>
        Math.Min(maximum, Math.Max(minimum, value));

    private static int Round(double value) =>
        (int)Math.Round(value, MidpointRounding.AwayFromZero);

    private static double RoundTenths(double value) =>
        Math.Round(value, 1, MidpointRounding.AwayFromZero);

    private readonly record struct FleetFactors(
        double Power,
        double ResistanceFactor,
        double InstabilityFactor,
        double GreenZoneFactor,
        double ChargeRateFactor,
        double ShatterFactor);
}
