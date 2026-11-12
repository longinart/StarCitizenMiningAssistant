using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.Core.Calculation;

/// <summary>
/// Percentage-point modifiers. Power 50 means the power multiplier is 1.50.
/// </summary>
public readonly record struct ModifierSet(
    double PowerPercent,
    double ResistancePercent,
    double InstabilityPercent,
    double GreenZonePercent,
    double ChargeRatePercent,
    double ShatterPercent)
{
    public static ModifierSet None { get; } = new(0, 0, 0, 0, 0, 0);

    public double PowerFactor => 1 + PowerPercent / 100;

    public double ResistanceFactor => 1 + ResistancePercent / 100;

    public double InstabilityFactor => 1 + InstabilityPercent / 100;

    public double GreenZoneFactor => 1 + GreenZonePercent / 100;

    public double ChargeRateFactor => 1 + ChargeRatePercent / 100;

    public double ShatterFactor => 1 + ShatterPercent / 100;
}

public readonly record struct LaserContribution(
    double MaxPower,
    double CraftPowerPercent,
    ModifierSet Head,
    IReadOnlyList<ModifierSet> PassiveModules)
{
    public LaserContribution(double maxPower)
        : this(maxPower, 0, ModifierSet.None, [])
    {
    }
}

public readonly record struct RockReading(
    ObservedValue<double> MassKilograms,
    ObservedValue<double> ResistancePercent,
    ObservedValue<double> Instability)
{
    public static RockReading Confirmed(double massKilograms, double resistancePercent, double instability) =>
        new(
            ObservedValue<double>.Confirmed(massKilograms),
            ObservedValue<double>.Confirmed(resistancePercent),
            ObservedValue<double>.Confirmed(instability));
}

public enum CrackBand
{
    Unknown = 0,
    NeedsMoreLasers = 1,
    NeedsGadgetOrConsumable = 2,
    Borderline = 3,
    Comfortable = 4,
    Trivial = 5
}

public sealed record CrackAssessment(
    string FormulaVersion,
    CrackBand Band,
    bool? CanCrack,
    double? PowerRequired,
    double? PowerDelivered,
    double? PowerRatio,
    double? EffectiveResistance,
    double? EffectiveInstability,
    double? GreenZoneWidth,
    int? RecommendedThrottlePercent,
    int? MaxCrackableMass,
    double? EstimatedCrackTimeSeconds,
    string Reason);
