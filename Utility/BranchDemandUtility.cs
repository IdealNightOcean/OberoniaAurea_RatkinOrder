using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.Branch;
using static OberoniaAurea.RatkinOrder.BranchDemand;

namespace OberoniaAurea.RatkinOrder;

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
        /*
        List<(BranchDemandDef, float)> demandsWithChance = new(demandOfType.Count);
        for (int i = 0; i < demandOfType.Count; i++)
        {
            demandsWithChance.AddMercyQuest((demandOfType[i], demandOfType[i].Weighter.GetDemandWeightOnly(branch)));
        }
        */

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

    public static bool CanAcceptDemand(Branch branch, BranchDemand demand)
    {
        if (branch is null || demand is null || demand.HasAccepted)
        {
            return false;
        }

        return true;

        /*
        OrderRelationshipKind restrictedRelation = demand.Def.demandType switch
        {
            BranchDemandType.Important => OrderRelationshipKind.Trustworthy,
            BranchDemandType.Core => OrderRelationshipKind.Soulmate,
            _ => OrderRelationshipKind.Acquaintance,
        };

        if (branch.IsBranchOfType(BranchType.Friendly))
        {
            restrictedRelation = restrictedRelation.RelationshipKindOffsetBy(-1);
        }

        if (branch.RatkinOrder.Relationship < restrictedRelation)
        {
            return false;
        }

        return true;
        */
    }


    public static void FriendyBranchDemandInform(Branch branch, BranchDemandDef demandDef)
    {
        bool showMessage = demandDef.IsCriticalDemand ? RatkinOrderSettings.CriticalDemandShowMess : RatkinOrderSettings.NoramlDemandShowMess;
        if (showMessage)
        {
            Messages.Message("OARO_Message_DemandFriendlyInform".Translate(branch.Name, demandDef.label), MessageTypeDefOf.PositiveEvent);
        }
        if (Rand.Bool && !branch.RatkinOrder.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.DemandFriendlyInform))
        {
            branch.RatkinOrder.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.DemandFriendlyInform, cdTicks: 12 * 60000, shouldRemoveWhenExpired: true);

            OrderLetterUtility.MakeOrderLetter(label: "OARO_LetterLabel_DemandFriendlyInform".Translate(branch.Name),
                                               text: "OARO_LetterLabel_DemandFriendlyInform".Translate(branch.Name, demandDef.label),
                                               letterType: OrderLetter.LetterType.Official,
                                               relatedOrder: branch.RatkinOrder,
                                               sender: branch.Name);
        }
    }
}
