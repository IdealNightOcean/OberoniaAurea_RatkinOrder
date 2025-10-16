using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_BuildingCeiling : BranchStatPart
{
    public override float PostTransform(Branch branch, float value)
    {
        value += branch.FacilityHandler.TotalFacilityLevel / 8;
        value += Mathf.Min(branch.PopulationHandler.Population / 2000, 2);
        return value;
    }

    public override void ModifyExplanation(Branch branch, StringBuilder explanation)
    {
        int offset = Mathf.FloorToInt(branch.FacilityHandler.TotalFacilityLevel / 8f);
        if (offset != 0)
        {
            explanation.Append("    ");
            explanation.AppendLine("OARO_StatOffset_FacilityLevel".Translate(offset.ToStringWithSign())
                                                                  .Colorize(Color.green));
        }
        offset = Mathf.Min(branch.PopulationHandler.Population / 2000, 2);
        if (offset != 0)
        {
            explanation.Append("    ");
            explanation.AppendLine("OARO_StatOffset_BranchPopulation".Translate(offset.ToStringWithSign())
                                                                     .Colorize(Color.green));
        }
    }
}