using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_OrderReformationFactor : BranchStatPart
{
    public OrderReformationDef reformation;
    public float factor;

    public override void PostTransform(Branch branch, ref float curValue)
    {
        if (branch.RatkinOrder.ReformationManager.HasReformation(reformation))
        {
            curValue *= factor;
        }
    }

    public override void ModifyExplanation(Branch branch, BranchStatDef statDef, StringBuilder explanation)
    {
        if (branch.RatkinOrder.ReformationManager.HasReformation(reformation))
        {
            explanation.Append("    ");
            if (statDef.statType == BranchStatDef.StatType.Percent)
            {
                explanation.AppendLine("OARO_ChangeFactor_Reformation".Translate(reformation.label, factor.ToStringPercentSigned("0.##"))
                                                                      .Colorize((factor < 1f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green));
            }
            else
            {
                explanation.AppendLine("OARO_ChangeFactor_Reformation".Translate(reformation.label, factor.ToStringWithSign("0.##"))
                                                                      .Colorize((factor < 1f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green));
            }

        }
    }
}