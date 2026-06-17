using NightOcean;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchInfoUICache : BranchSummaryUICache
{
    public int PopulationCeiling { get; private set; }
    public int BuildingCeiling { get; private set; }

    public LazyMutable<int> DailyPopulationGrowth_Bottom { get; }
    public LazyMutable<int> DailyPopulationGrowth_Ceiling { get; }

    public LazyMutable<string> DailyPopulationGrowthExplanation { get; }

    public BranchInfoUICache(Branch branch, Map map) : base(branch, map)
    {
        PopulationCeiling = (int)branch.GetStatValue(BranchStatDefOf.OARO_NaturalPopulationCeiling, immediateUpdate: true);
        BuildingCeiling = (int)branch.GetStatValue(BranchStatDefOf.OARO_BuildingCeiling, immediateUpdate: true);

        DailyPopulationGrowth_Bottom = new(refreshFunc: () =>
        {
            float growth = Branch.GetStatValue(BranchStatDefOf.OARO_DailyPopulationGrowth);
            return Mathf.CeilToInt(growth * 0.5f);
        });
        DailyPopulationGrowth_Ceiling = new(refreshFunc: () =>
        {
            float growth = Branch.GetStatValue(BranchStatDefOf.OARO_DailyPopulationGrowth);
            return Mathf.FloorToInt(growth * 1.5f);
        });
        DailyPopulationGrowthExplanation = new(refreshFunc: GetDailyPopulationGrowthExplanation);
    }

    /// <summary>
    /// 标记所有可变缓存为脏
    /// </summary>
    public void MarkDirty()
    {
        PopulationCeiling = (int)Branch.GetStatValue(BranchStatDefOf.OARO_NaturalPopulationCeiling, immediateUpdate: true);
        BuildingCeiling = (int)Branch.GetStatValue(BranchStatDefOf.OARO_BuildingCeiling, immediateUpdate: true);
        DailyPopulationGrowth_Bottom.MarkDirty();
        DailyPopulationGrowth_Ceiling.MarkDirty();
        DailyPopulationGrowthExplanation.MarkDirty();
    }

    private string GetDailyPopulationGrowthExplanation()
    {
        try
        {
            StringBuilder growthExplanation = BranchStatUtility.GetStatModifyExplanation(Branch, BranchStatDefOf.OARO_DailyPopulationGrowth, showResultValue: false);

            int bottom = DailyPopulationGrowth_Bottom.Value;
            int ceiling = DailyPopulationGrowth_Ceiling.Value;
            if (bottom > ceiling)
            {
                (bottom, ceiling) = (ceiling, bottom);
            }
            growthExplanation.Append("    ");
            growthExplanation.AppendLine("OARO_ExtraPopulationGrowthFloat".Translate());
            growthExplanation.AppendLine("OARO_FinalPopulationGrowth".Translate(bottom.ToString(), ceiling.ToString())
                                                                     .Colorize(bottom > 0 ? Color.green : ColorLibrary.RedReadable));
            return growthExplanation.ToString();
        }
        catch
        {
            return "ERROR (´;ω;`)".Colorize(ColorLibrary.RedReadable);
        }
    }
}