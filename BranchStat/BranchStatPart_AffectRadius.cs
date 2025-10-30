using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_AffectRadius : BranchStatPart
{
    public override void PostTransform(Branch branch, ref float curValue)
    {
        curValue += branch.RatkinOrder.FundHandler.Funds / 0.08f;

        if (branch.RatkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder))
        {
            curValue += 10f;
        }
    }

    public override void ModifyExplanation(Branch branch, StringBuilder explanation)
    {
        explanation.Append("    ");
        explanation.AppendLine("OARO_ChangeOffset_Fund".Translate((branch.RatkinOrder.FundHandler.Funds / 0.08f).ToStringWithSign("0.##"))
                                                       .Colorize(Color.green));

        if (branch.RatkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder))
        {
            explanation.Append("    ");
            explanation.AppendLine("OARO_ChangeOffset_Reformation".Translate(OrderReformationDefOf.OARO_ReformationPlaceholder.label, 10.ToStringWithSign())
                                                                  .Colorize(Color.green));
        }
    }
}