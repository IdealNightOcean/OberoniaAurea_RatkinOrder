using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_NaturalPopulationCeiling : BranchStatPart
{
    public override void PostTransform(Branch branch, ref float curValue)
    {
        curValue += branch.FacilityHandler.TotalFacilityLevel.Value * 200;

        if (branch.RatkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder))
        {
            curValue += 1000f;
        }
    }

    public override void ModifyExplanation(Branch branch, StringBuilder explanation)
    {
        explanation.Append("    ");
        explanation.AppendLine("OARO_ChangeOffset_FacilityLevel".Translate((branch.FacilityHandler.TotalFacilityLevel.Value * 200).ToStringWithSign())
                                                                .Colorize(Color.green));

        if (branch.RatkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder))
        {
            explanation.Append("    ");
            explanation.AppendLine("OARO_ChangeOffset_Reformation".Translate(OrderReformationDefOf.OARO_ReformationPlaceholder.label, 1000.ToStringWithSign())
                                                                  .Colorize(Color.green));
        }
    }
}