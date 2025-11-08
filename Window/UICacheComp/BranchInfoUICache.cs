using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchInfoUICache : BranchSummaryUICache
{
    public int PopulationCeiling;
    public int BuildingCeiling;

    public int DailyPopulationGrowth_Bottom;
    public int DailyPopulationGrowth_Ceiling;
    public string DailyPopulationGrowthExplanation;

    public BranchInfoUICache() : base() { }

    public BranchInfoUICache(Branch branch, Map map) : base(branch, map)
    {
        PopulationCeiling = (int)branch.GetStatValue(BranchStatDefOf.OARO_NaturalPopulationCeiling);

        try
        {
            BuildingCeiling = (int)branch.GetStatValue(BranchStatDefOf.OARO_BuildingCeiling);
        }
        catch
        {

        }

        try
        {

            StringBuilder growthExplanation = BranchStatUtility.GetStatModifyExplanation(branch, BranchStatDefOf.OARO_DailyPopulationGrowth);
            int dailyPopulationGrowth = (int)branch.GetStatValue(BranchStatDefOf.OARO_DailyPopulationGrowth);
            DailyPopulationGrowth_Bottom = (int)(dailyPopulationGrowth * 0.75f);
            DailyPopulationGrowth_Ceiling = (int)(dailyPopulationGrowth * 1.25f);

            growthExplanation.AppendLine("OARO_ExtraPopulationGrowthFloat".Translate(DailyPopulationGrowth_Bottom.ToString(), DailyPopulationGrowth_Ceiling.ToString())
                                                                          .Colorize(dailyPopulationGrowth > 0f ? Color.green : ColorLibrary.RedReadable));

        }
        catch
        {
            DailyPopulationGrowthExplanation = "ERROR".Colorize(ColorLibrary.RedReadable);
        }
    }
}