using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_BuildingCeiling : BranchStatPart
{
    public override void PostTransform(Branch branch, ref float curValue)
    {
        curValue += branch.FacilityHandler.TotalFacilityLevel / 8;
        curValue += Mathf.Min(branch.PopulationHandler.Population / 2000, 2);
        if (branch.RatkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder))
        {
            curValue += 1f;
        }
    }

    public override void ModifyExplanation(Branch branch, StringBuilder explanation)
    {
        int offset = Mathf.FloorToInt(branch.FacilityHandler.TotalFacilityLevel / 8f);
        if (offset > 0)
        {
            explanation.Append("    ");
            explanation.AppendLine("OARO_ChangeOffset_FacilityLevel".Translate(offset.ToStringWithSign())
                                                                    .Colorize(Color.green));
        }
        offset = Mathf.Min(branch.PopulationHandler.Population / 2000, 2);
        if (offset > 0)
        {
            explanation.Append("    ");
            explanation.AppendLine("OARO_ChangeOffset_BranchPopulation".Translate(offset.ToStringWithSign())
                                                                       .Colorize(Color.green));
        }
        if (branch.RatkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder))
        {
            explanation.Append("    ");
            explanation.AppendLine("OARO_ChangeOffset_Reformation".Translate(OrderReformationDefOf.OARO_ReformationPlaceholder.label, 1.ToStringWithSign())
                                                                  .Colorize(Color.green));
        }
    }
}