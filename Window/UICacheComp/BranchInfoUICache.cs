using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchInfoUICache : BranchSummaryUICache
{
    public int PopulationCeiling { get; }
    public int BuildingCeiling { get; }

    public int DailyPopulationGrowth_Bottom { get; set; }
    public int DailyPopulationGrowth_Ceiling { get; set; }
    private string dailyPopulationGrowthExplanation;
    public string DailyPopulationGrowthExplanation => dailyPopulationGrowthExplanation ??= GetDailyPopulationGrowthExplanation();

    public BranchInfoUICache(Branch branch, Map map) : base(branch, map)
    {
        PopulationCeiling = (int)branch.GetStatValue(BranchStatDefOf.OARO_NaturalPopulationCeiling);
        BuildingCeiling = (int)branch.GetStatValue(BranchStatDefOf.OARO_BuildingCeiling);

        float dailyPopulationGrowth = Branch.GetStatValue(BranchStatDefOf.OARO_DailyPopulationGrowth);
        DailyPopulationGrowth_Bottom = Mathf.CeilToInt(dailyPopulationGrowth * 0.5f);
        DailyPopulationGrowth_Ceiling = Mathf.FloorToInt(dailyPopulationGrowth * 1.5f);
        if (DailyPopulationGrowth_Bottom > DailyPopulationGrowth_Ceiling)
        {
            (DailyPopulationGrowth_Bottom, DailyPopulationGrowth_Ceiling) = (DailyPopulationGrowth_Ceiling, DailyPopulationGrowth_Bottom);
        }
    }

    private string GetDailyPopulationGrowthExplanation()
    {
        try
        {
            StringBuilder growthExplanation = BranchStatUtility.GetStatModifyExplanation(Branch, BranchStatDefOf.OARO_DailyPopulationGrowth, showResultValue: false);

            float dailyPopulationGrowth = Branch.GetStatValue(BranchStatDefOf.OARO_DailyPopulationGrowth);
            DailyPopulationGrowth_Bottom = Mathf.CeilToInt(dailyPopulationGrowth * 0.5f);
            DailyPopulationGrowth_Ceiling = Mathf.FloorToInt(dailyPopulationGrowth * 1.5f);
            if (DailyPopulationGrowth_Bottom > DailyPopulationGrowth_Ceiling)
            {
                (DailyPopulationGrowth_Bottom, DailyPopulationGrowth_Ceiling) = (DailyPopulationGrowth_Ceiling, DailyPopulationGrowth_Bottom);
            }
            growthExplanation.AppendLine("OARO_ExtraPopulationGrowthFloat".Translate());
            growthExplanation.AppendLine("OARO_FinalPopulationGrowth".Translate(DailyPopulationGrowth_Bottom.ToString(), DailyPopulationGrowth_Ceiling.ToString())
                                                                     .Colorize(dailyPopulationGrowth > 0f ? Color.green : ColorLibrary.RedReadable));
            return growthExplanation.ToString();
        }
        catch
        {
            return "ERROR (£»¡ä¡Ð`)".Colorize(ColorLibrary.RedReadable);
        }
    }
}