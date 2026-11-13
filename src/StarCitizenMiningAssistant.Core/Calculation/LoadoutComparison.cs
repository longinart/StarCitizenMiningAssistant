using System.Globalization;
using StarCitizenMiningAssistant.Core.Equipment;

namespace StarCitizenMiningAssistant.Core.Calculation;

public enum CrackVerdict
{
    Unknown = 0,
    Uncrackable = 1,
    Difficult = 2,
    Crackable = 3
}

/// <summary>
/// One saved loadout against one rock. Boosts names the active modules and
/// gadget that the crack needs. Instability and the charge window come from
/// that same check. They do not change the verdict.
/// </summary>
public sealed record LoadoutCrackRow(
    string Name,
    CrackVerdict Verdict,
    string Boosts,
    double? Instability,
    double? ChargeWindow,
    string Reason);

public static class LoadoutComparison
{
    public static IReadOnlyList<LoadoutCrackRow> Compare(
        MiningCatalog catalog,
        RockReading rock,
        IReadOnlyList<StoredLoadout> loadouts)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(loadouts);
        return loadouts.Select(loadout => CompareOne(catalog, rock, loadout)).ToList();
    }

    private static LoadoutCrackRow CompareOne(MiningCatalog catalog, RockReading rock, StoredLoadout loadout)
    {
        if (!TryBuild(catalog, loadout, out var equipped, out var withoutActives, out var activeNames, out var gadget, out var problem))
        {
            return new LoadoutCrackRow(loadout.Name, CrackVerdict.Unknown, "", null, null, problem);
        }

        var gadgetModifier = gadget is null ? (ModifierSet?)null : GadgetModifier(gadget);
        var bare = CrackabilityCalculator.Assess(rock, withoutActives);
        var gadgetOnly = gadgetModifier is null
            ? null
            : CrackabilityCalculator.Assess(rock, withoutActives, gadgetModifier);
        var withActives = activeNames.Count == 0
            ? null
            : CrackabilityCalculator.Assess(rock, equipped);
        var both = activeNames.Count > 0 && gadgetModifier is not null
            ? CrackabilityCalculator.Assess(rock, equipped, gadgetModifier)
            : null;

        if (bare.Band == CrackBand.Unknown)
        {
            return Row(loadout.Name, CrackVerdict.Unknown, "", bare);
        }

        if (bare.CanCrack == true)
        {
            return Row(loadout.Name, CrackVerdict.Crackable, "", bare);
        }

        var gadgetCracks = gadgetOnly?.CanCrack == true;
        var activeCracks = withActives?.CanCrack == true;
        var bothCrack = both?.CanCrack == true;
        var gadgetName = gadget?.Name ?? "Gadget";
        var activeText = string.Join(" + ", activeNames);

        if (gadgetCracks && activeCracks)
        {
            return Row(loadout.Name, CrackVerdict.Crackable, gadgetName + " or " + activeText, gadgetOnly!);
        }

        if (gadgetCracks)
        {
            return Row(loadout.Name, CrackVerdict.Crackable, gadgetName, gadgetOnly!);
        }

        if (activeCracks)
        {
            return Row(loadout.Name, CrackVerdict.Crackable, activeText, withActives!);
        }

        if (bothCrack)
        {
            return Row(loadout.Name, CrackVerdict.Crackable, activeText + " + " + gadgetName, both!);
        }

        var fullest = both ?? withActives ?? gadgetOnly ?? bare;
        var verdict = fullest.Band == CrackBand.NeedsGadgetOrConsumable
            ? CrackVerdict.Difficult
            : CrackVerdict.Uncrackable;
        return Row(loadout.Name, verdict, "", fullest);
    }

    private static LoadoutCrackRow Row(string name, CrackVerdict verdict, string boosts, CrackAssessment assessment) =>
        new(name, verdict, boosts, assessment.EffectiveInstability, assessment.GreenZoneWidth, assessment.Reason);

    private static bool TryBuild(
        MiningCatalog catalog,
        StoredLoadout loadout,
        out List<LaserContribution> equipped,
        out List<LaserContribution> withoutActives,
        out List<string> activeNames,
        out MiningGadget? gadget,
        out string problem)
    {
        equipped = [];
        withoutActives = [];
        activeNames = [];
        gadget = null;
        problem = "";

        if (!string.IsNullOrWhiteSpace(loadout.GadgetId))
        {
            gadget = catalog.FindGadget(loadout.GadgetId);
            if (gadget is null)
            {
                problem = "The gadget is not in the catalog.";
                return false;
            }
        }

        foreach (var turret in loadout.Turrets ?? [])
        {
            if (!turret.LaserOn)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(turret.HeadId))
            {
                problem = "A switched-on laser has no mining head.";
                return false;
            }

            var head = catalog.FindHead(turret.HeadId);
            if (head is null)
            {
                problem = "The mining head is not in the catalog.";
                return false;
            }

            if (!TryParseCraft(turret.CraftPercent, out var craft))
            {
                problem = "Craft percent has to be a number.";
                return false;
            }

            var all = new List<ModifierSet>();
            var passive = new List<ModifierSet>();
            foreach (var moduleId in turret.Modules ?? [])
            {
                if (string.IsNullOrWhiteSpace(moduleId))
                {
                    continue;
                }

                var module = catalog.FindModule(moduleId);
                if (module is null)
                {
                    problem = "A module is not in the catalog.";
                    return false;
                }

                var modifier = ModuleModifier(module);
                all.Add(modifier);
                if (module.Active)
                {
                    activeNames.Add(module.Name);
                }
                else
                {
                    passive.Add(modifier);
                }
            }

            var headModifier = new ModifierSet(0, head.Resistance, head.Instability, head.GreenZone, head.OptimalChargeRate, 0);
            equipped.Add(new LaserContribution(head.MaxPower, craft, headModifier, all));
            withoutActives.Add(new LaserContribution(head.MaxPower, craft, headModifier, passive));
        }

        return true;
    }

    private static ModifierSet ModuleModifier(MiningModule module) =>
        new(module.PowerPercent, module.Resistance, module.Instability, module.GreenZone, module.OptimalChargeRate, module.Shatter);

    private static ModifierSet GadgetModifier(MiningGadget gadget) =>
        new(gadget.PowerPercent, gadget.Resistance, gadget.Instability, gadget.GreenZone, gadget.OptimalChargeRate, gadget.Shatter);

    private static bool TryParseCraft(string? text, out double value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0;
            return true;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
        {
            return true;
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
