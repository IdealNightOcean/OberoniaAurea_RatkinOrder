using NightOcean;
using OberoniaAurea_Frame;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIData_BrnchInfo : UIData_SquadSummary
{
    public int PopulationCeiling { get; protected set; }
    public int BuildingCeiling { get; protected set; }

    public float DailyPopulationGrowth { get; protected set; }

    public (int bottom, int ceiling) DailyPopulationGrowthBoundary
    {
        get
        {
            int bottom = Mathf.CeilToInt(DailyPopulationGrowth * 0.5f);
            int ceiling = Mathf.CeilToInt(BuildingCeiling * 1.5f);
            if (bottom > ceiling)
            {
                (bottom, ceiling) = (ceiling, bottom);
            }
            return (bottom, ceiling);
        }
    }

    public LazyMutable<string> DailyPopulationGrowthExplanation { get; }

    public UIData_BrnchInfo(Branch branch, Map map) : base(branch, map)
    {
        DailyPopulationGrowthExplanation = new(refreshFunc: GetDailyPopulationGrowthExplanation);
    }

    protected override void RefreshInner()
    {
        base.RefreshInner();
        PopulationCeiling = (int)Branch.GetStatValue(BranchStatDefOf.OARO_NaturalPopulationCeiling, immediateUpdate: true);
        BuildingCeiling = (int)Branch.GetStatValue(BranchStatDefOf.OARO_BuildingCeiling, immediateUpdate: true);
        DailyPopulationGrowth = (int)Branch.GetStatValue(BranchStatDefOf.OARO_DailyPopulationGrowth, immediateUpdate: true);

        DailyPopulationGrowthExplanation.MarkDirty();
    }

    private string GetDailyPopulationGrowthExplanation()
    {
        try
        {
            (string growthExplanation, float? resultNullabel) = BranchStatDefOf.OARO_DailyPopulationGrowth.GetStatModifyExplanation(new BranchStatRequestData(this.Branch));

            DailyPopulationGrowth = resultNullabel ?? BranchStatDefOf.OARO_DailyPopulationGrowth.baseValue;

            StringBuilder growthExplanationBuilder = new(growthExplanation);
            growthExplanationBuilder.Append("    ");
            growthExplanationBuilder.AppendLine("OARO_ExtraPopulationGrowthFloat".Translate());
            growthExplanationBuilder.AppendLine("OARO_FinalPopulationGrowth".Translate(DailyPopulationGrowthBoundary.bottom.ToString(), DailyPopulationGrowthBoundary.ceiling.ToString())
                                                                            .Colorize(DailyPopulationGrowth > 0 ? Color.green : ColorLibrary.RedReadable));
            return growthExplanation.ToString();
        }
        catch
        {
            return KeyLibrary_Misc.ErrorTipWithColor;
        }
    }
}