using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

using static OberoniaAurea.RatkinOrder.Branch;
using static OberoniaAurea.RatkinOrder.BranchDemand;

public static class BranchDemandUtility
{
    public static BranchDemandDef GetRandomBranchDemandOfType(Branch branch, DemandType demandType)
    {
        return GetBranchDemandOfTypeWithWeight(branch, demandType).RandomElementByWeightWithFallback(pair => pair.Item2, fallback: (null, 1f)).Item1;
    }

    public static List<(BranchDemandDef, float)> GetBranchDemandOfTypeWithWeight(Branch branch, DemandType demandType)
    {
        List<BranchDemandDef> demandOfType = DefDatabase<BranchDemandDef>.AllDefsListForReading.Where(d => d.demandType == demandType).ToList();

        List<(BranchDemandDef def, float)> demandsWithChance = demandOfType.AsParallel()
                                                                           .Select(def => (def, def.Weighter.GetDemandWeightOnly(def, branch)))
                                                                           .ToList();
        return demandsWithChance;
    }

    public static string GetBranchDemandWithOfTypeWeightExplain(Branch branch, DemandType demandType)
    {
        List<(BranchDemandDef, float)> demandsWithChance = GetBranchDemandOfTypeWithWeight(branch, demandType);
        if (demandsWithChance.NullOrEmpty())
        {
            return "None".Translate();
        }

        float totalWeight = demandsWithChance.Sum(dc => dc.Item2);

        StringBuilder sb = new();
        foreach ((BranchDemandDef, float) pair in demandsWithChance)
        {
            sb.AppendInNewLine($"{pair.Item1.label}: {(pair.Item2 / totalWeight).ToStringPercent("0.##")}");
        }
        return sb.ToString();
    }

    public static float GetCriticalDemandTriggerChance(Branch branch, bool resultOnly, out string explain)
    {
        explain = string.Empty;
        if (branch.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.CriticalDemandAdd))
        {
            if (!resultOnly)
            {
                explain = "OARO_Cooling_BranchCriticalDemandAdd".Translate();
            }
            return 0f;
        }

        List<(string key, Func<float> calculator)> rules =
        [
            ("OARO_BranchCriticalDemandAdd_Base", () => 0.05f),
            ("OARO_ChangeOffset_HonorBranch", () => branch.IsBranchOfType(BranchType.Honor) ? 0.02f : 0f),
            ("OARO_BranchCriticalDemandAdd_Facility", () => branch.FacilityHandler.TotalFacilityLevel * 0.005f),
            ("OARO_BranchCriticalDemandAdd_Medal", () => branch.MedalHandler.TotalMedalCount * 0.005f),
            ("OARO_BranchCriticalDemandAdd_Member", () => (1f - branch.Squad.MemberPercentage) * 0.05f),
            ("OARO_BranchCriticalDemandAdd_Fund", () => (1f - branch.RatkinOrder.Funds) * 0.1f),
        ];

        StringBuilder sb = resultOnly ? null : new();
        float chance = 0f;
        float stepChange;

        foreach ((string key, Func<float> calculator) in rules)
        {
            stepChange = calculator.Invoke();
            chance += stepChange;
            if (!resultOnly && stepChange != 0f)
            {
                sb.AppendInNewLine(key.Translate(stepChange.ToStringPercentSigned("0.##")).Colorize(stepChange >= 0f ? Color.green : ColorLibrary.RedReadable));
            }
        }

        if (chance < 0.02f || chance > 0.15f)
        {
            chance = Mathf.Clamp(chance, 0.02f, 0.15f);
            if (!resultOnly)
            {
                sb.AppendInNewLine("OARO_BranchCriticalDemandAdd_Restoration".Translate(0.02f.ToStringPercent("F0"), 0.15f.ToStringPercent("F0")));
            }
        }

        if (!resultOnly)
        {
            explain = sb.ToString();
        }
        return chance;
    }

    public static bool TryAddRandomDemandToBranch(out BranchDemandDef demandDef, Branch branch, DemandType demandType, bool ignoreCD = false, bool replaceCur = false)
    {
        demandDef = null;

        if (branch is null || !branch.DemandHandler.CanAddDemand(isCriticalDemand: demandType == DemandType.Critical, ignoreCD, replaceCur))
        {
            return false;
        }

        demandDef = GetRandomBranchDemandOfType(branch, demandType);
        if (demandDef is null)
        {
            return false;
        }
        branch.DemandHandler.AddNewDemand(demandDef);
        return true;
    }

    public static AcceptanceReport CanAcceptDemand(Branch branch, bool isCritical, bool resultOnly)
    {
        if (branch is null)
        {
            return false;
        }
        BranchDemand demand = branch.DemandHandler.GetDemand(isCritical);
        if (demand is null)
        {
            return false;
        }
        if (demand.HasAccepted)
        {
            return resultOnly ? false : "OARO_HasAccepted".Translate();
        }

        if (AcceptedBranchDemandHandler.Instance.AcceptanceCount >= RatkinOrderSettings.MaxConcurrentAcceptedDemand)
        {
            return resultOnly ? false : "OARO_ReachMax_ConcurrentAcceptedDemand".Translate();
        }

        EsteemHandler.RelationshipKind restrictedRelation = isCritical ? EsteemHandler.RelationshipKind.Trustworthy
                                                                       : EsteemHandler.RelationshipKind.Acquaintance;

        if (branch.IsBranchOfType(BranchType.Friendly))
        {
            restrictedRelation = restrictedRelation.RelationshipKindOffsetBy(-1);
        }

        if (branch.RatkinOrder.Relationship < restrictedRelation)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(restrictedRelation.GetLabel());
        }

        return true;
    }
}