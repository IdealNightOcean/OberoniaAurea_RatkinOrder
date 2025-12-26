using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class BranchConstructUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetFacilityLevelLabel(this BranchFacilityLevel level) => $"OARO_BranchFacilityLevel_{level}".Translate();

    public static int GetBuildingSilverCost(this Branch branch, BranchBuildingDef buildingDef, bool resultOnly, out string explanation)
    {
        explanation = string.Empty;
        StringBuilder explanationSB = resultOnly ? null : new(64);
        float cost = buildingDef.silverCost;
        if (!resultOnly)
        {
            explanationSB.Append("- ");
            explanationSB.AppendLine("OARO_BuildSilverCost_Base".Translate(buildingDef.silverCost));
        }

        float costFactor = branch.GetStatValue(BranchStatDefOf.OARO_ConstructionCostFactor);
        if (costFactor != 1f)
        {
            cost *= costFactor;
            if (!resultOnly)
            {
                explanationSB.AppendLine();
                explanationSB.Append("- ");
                explanationSB.AppendLine("OARO_BuildSilverCost_CostFactor".Translate(costFactor.ToStringPercent("F1")).Colorize(costFactor < 1f ? Color.green : ColorLibrary.RedReadable));
                StringBuilder statExplanation = BranchStatUtility.GetStatModifyExplanation(branch, BranchStatDefOf.OARO_ConstructionCostFactor, showResultValue: false);
                explanationSB.Append(statExplanation);
            }
        }
        costFactor = branch.StoresReserveHandler.GetReserveCostReduce(buildingDef);
        if (costFactor != 0f)
        {
            cost *= (1f + costFactor);
            if (!resultOnly)
            {
                explanationSB.AppendLine();
                explanationSB.Append("- ");
                explanationSB.AppendLine("OARO_BuildSilverCost_ReserveReduction".Translate(costFactor.ToStringPercent("F1")).Colorize(Color.green));

            }
        }
        if (branch.PopulationHandler.Population < buildingDef.suggestedMinPopulation)
        {
            cost *= 2f;
            if (!resultOnly)
            {
                explanationSB.AppendLine();
                explanationSB.Append("- ");
                explanationSB.AppendLine("OARO_BuildSilverCost_InsufficientPopulation".Translate(2f.ToStringPercent("F0")).Colorize(ColorLibrary.RedReadable));
            }
        }

        if (!resultOnly)
        {
            explanationSB.AppendLine();
            explanationSB.Append("- ");
            explanationSB.AppendLine("OARO_BuildSilverCost_FinalCost".Translate(cost.ToString("F0")));
            explanation = explanationSB.ToString();
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

    public static int GetFacilitySilverCost(this Branch branch, BranchFacilityDef facilityDef, BranchFacilityLevel targetLevel, bool resultOnly, out string explanation)
    {
        explanation = string.Empty;
        StringBuilder explanationSB = resultOnly ? null : new(64);
        float cost = facilityDef.GetLevelStage(targetLevel)?.silverCost ?? 2000f;
        if (!resultOnly)
        {
            explanationSB.Append("- ");
            explanationSB.AppendLine("OARO_BuildSilverCost_Base".Translate(cost.ToString("F0")));
        }

        float costFactor = branch.GetStatValue(BranchStatDefOf.OARO_ConstructionCostFactor);
        if (costFactor != 1f)
        {
            cost *= costFactor;
            if (!resultOnly)
            {
                explanationSB.AppendLine();
                explanationSB.Append("- ");
                explanationSB.AppendLine("OARO_BuildSilverCost_CostFactor".Translate(costFactor.ToStringPercent("F1")).Colorize(costFactor < 1f ? Color.green : ColorLibrary.RedReadable));
                StringBuilder statExplanation = BranchStatUtility.GetStatModifyExplanation(branch, BranchStatDefOf.OARO_ConstructionCostFactor, showResultValue: false);
                explanationSB.Append(statExplanation);
            }
        }
        costFactor = branch.StoresReserveHandler.GetReserveCostReduce(facilityDef);
        if (costFactor != 0f)
        {
            cost *= (1f + costFactor);
            if (!resultOnly)
            {
                explanationSB.AppendLine();
                explanationSB.Append("- ");
                explanationSB.AppendLine("OARO_BuildSilverCost_ReserveReduction".Translate(costFactor.ToStringPercent("F1")).Colorize(Color.green));
            }
        }

        if (!resultOnly)
        {
            explanationSB.AppendLine();
            explanationSB.Append("- ");
            explanationSB.AppendLine("OARO_BuildSilverCost_FinalCost".Translate(cost.ToString("F0")));
            explanation = explanationSB.ToString();
        }

        return Mathf.RoundToInt(cost < 0f ? 0f : cost);
    }

    public static int GetFacilityTimeCost(this Branch branch, BranchFacilityDef facilityDef, BranchFacilityLevel targetLevel)
    {
        float cost = (facilityDef.GetLevelStage(targetLevel)?.constructionDays ?? 7) * 60000f / branch.GetStatValue(BranchStatDefOf.OARO_ConstructionSpeedFactor);

        return Mathf.RoundToInt(cost < 0f ? 0f : cost);
    }
}
