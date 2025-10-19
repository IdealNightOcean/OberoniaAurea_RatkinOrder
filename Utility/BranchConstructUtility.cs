using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder;

public static class BranchConstructUtility
{
    public static int GetBuildingSilverCost(this Branch branch, BranchBuildingDef buildingDef)
    {
        float cost = buildingDef.silverCost * branch.GetStatValue(BranchStatDefOf.OARO_ConstructionCostFactor);
        cost *= (1f - branch.StoresReserveHandler.GetReserveCostReduce(buildingDef));
        if (branch.PopulationHandler.Population < buildingDef.suggestedMinPopulation)
        {
            cost *= 2f;
        }

        return Mathf.RoundToInt(cost < 0f ? 0f : cost);
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

    public static int GetFacilitySilverCost(this Branch branch, BranchFacilityDef facilityDef, BranchFacilityLevel targetLevel)
    {
        float cost = (facilityDef.GetLevelStage(targetLevel)?.silverCost ?? 2000) * branch.GetStatValue(BranchStatDefOf.OARO_ConstructionCostFactor);
        cost *= (1f - branch.StoresReserveHandler.GetReserveCostReduce(facilityDef));

        return Mathf.RoundToInt(cost < 0f ? 0f : cost);
    }

    public static int GetFacilityTimeCost(this Branch branch, BranchFacilityDef facilityDef, BranchFacilityLevel targetLevel)
    {
        float cost = (facilityDef.GetLevelStage(targetLevel)?.constructionDays ?? 7) * 60000f / branch.GetStatValue(BranchStatDefOf.OARO_ConstructionSpeedFactor);

        return Mathf.RoundToInt(cost < 0f ? 0f : cost);
    }

    public static string GetBuildSilverCostExplanation(Branch branch, BranchBuildingDef buildingDef)
    {
        throw new NotImplementedException();
    }
}
