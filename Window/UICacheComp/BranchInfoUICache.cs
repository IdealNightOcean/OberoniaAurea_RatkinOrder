using System;
using System.Text;
using UnityEngine;
using Verse;
using static RimWorld.RiverDef;

namespace OberoniaAurea.RatkinOrder;

public class BranchInfoUICache : BranchSummaryUICache
{
    public int PopulationCeiling;
    public int BuildingCeiling;

    public int DailyPopulationGrowth_Bottom;
    public int DailyPopulationGrowth_Ceiling;
    private string dailyPopulationGrowthExplanation;
    public string DailyPopulationGrowthExplanation => dailyPopulationGrowthExplanation ??= GetDailyPopulationGrowthExplanation();

    public BranchInfoUICache(Branch branch, Map map) : base(branch, map)
    {
        PopulationCeiling = (int)branch.GetStatValue(BranchStatDefOf.OARO_NaturalPopulationCeiling);
        BuildingCeiling = (int)branch.GetStatValue(BranchStatDefOf.OARO_BuildingCeiling);
    }

    private string GetDailyPopulationGrowthExplanation()
    {
        try
        {
            StringBuilder growthExplanation = BranchStatUtility.GetStatModifyExplanation(Branch, BranchStatDefOf.OARO_DailyPopulationGrowth);
            int dailyPopulationGrowth = (int)Branch.GetStatValue(BranchStatDefOf.OARO_DailyPopulationGrowth);
            growthExplanation.AppendLine("OARO_ExtraPopulationGrowthFloat".Translate(DailyPopulationGrowth_Bottom.ToString(), DailyPopulationGrowth_Ceiling.ToString())
                                                                          .Colorize(dailyPopulationGrowth > 0f ? Color.green : ColorLibrary.RedReadable));
            return growthExplanation.ToString();
        }
        catch
        {
            return "ERROR (£»¡ä¡Ð`)".Colorize(ColorLibrary.RedReadable);
        }
    }
}