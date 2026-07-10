using RimWorld;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;


public static class BranchConstructUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetFacilityLevelLabel(this BranchFacilityLevel level) => $"OARO_BranchFacilityLevel_{level}".Translate();

    public static int GetBuildingSilverCost(this Branch branch,
                                            BranchBuildingDef buildingDef,
                                            bool resultOnly,
                                            out string explanation)
    {
        explanation = string.Empty;
        float? costValue;

        BranchStatRequestData_BranchConstruction<BranchBuildingDef> statRequest = new(
            branch: branch,
            statDef: BranchStatDefOf.OARO_BranchBuildingCost,
            constructionDef: buildingDef);

        if (resultOnly)
        {
            costValue = Mathf.RoundToInt(statRequest.GetStatValue());
        }
        else
        {
            (explanation, costValue) = BranchStatDefOf.OARO_BranchBuildingCost.GetStatModifyExplanation(statRequest);
        }

        return Mathf.RoundToInt(costValue?? 0f);
    }

    public static int GetBuildingTimeCost(this Branch branch, BranchBuildingDef buildingDef)
    {
        float cost = buildingDef.constructionDays * 60000f / branch.GetStatValue(BranchStatDefOf.OARO_ConstructionSpeedFactor);
        return Mathf.RoundToInt(cost < 0f ? 0f : cost);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BranchFacilityLevel FacilityLevelOffSetBy(this BranchFacilityLevel level, int offset)
    {
        return (BranchFacilityLevel)Mathf.Clamp((int)level + offset, 0, 4);
    }

    public static int GetFacilitySilverCost(this Branch branch,
                                            BranchFacilityDef facilityDef,
                                            BranchFacilityLevel targetLevel,
                                            bool resultOnly,
                                            out string explanation)
    {
        explanation = string.Empty;
        float? costValue;

        BranchStatRequestData_BranchFacility statRequest = new(
            branch: branch,
            statDef: BranchStatDefOf.OARO_BranchFacilityCost,
            facilityDef: facilityDef,
            facilityLevel: targetLevel);

        if (resultOnly)
        {
            costValue = Mathf.RoundToInt(statRequest.GetStatValue());
        }
        else
        {
            (explanation, costValue) = BranchStatDefOf.OARO_BranchFacilityCost.GetStatModifyExplanation(statRequest);
        }

        return Mathf.RoundToInt(costValue ?? 0f);
    }

    public static int GetFacilityTimeCost(this Branch branch, BranchFacilityDef facilityDef, BranchFacilityLevel targetLevel)
    {
        float cost = (facilityDef.GetLevelStage(targetLevel)?.constructionDays ?? 7) * 60000f / branch.GetStatValue(BranchStatDefOf.OARO_ConstructionSpeedFactor);

        return Mathf.RoundToInt(cost < 0f ? 0f : cost);
    }
}
