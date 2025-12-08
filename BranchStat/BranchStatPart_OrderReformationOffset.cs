using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_OrderReformationOffset : BranchStatPart
{
    public OrderReformationDef reformation;
    public float offset;

    public override void PostTransform(Branch branch, ref float curValue)
    {
        if (branch.RatkinOrder.ReformationManager.HasReformation(reformation))
        {
            curValue += offset;
        }
    }

    public override void ModifyExplanation(Branch branch, BranchStatDef statDef, StringBuilder explanation)
    {
        if (branch.RatkinOrder.ReformationManager.HasReformation(reformation))
        {
            explanation.Append("    ");
            if (statDef.statType == BranchStatDef.StatType.Percent)
            {
                explanation.AppendLine("OARO_ChangeOffset_Reformation".Translate(reformation.label, offset.ToStringPercentSigned("0.##"))
                                                                      .Colorize((offset < 0f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green));
            }
            else
            {
                explanation.AppendLine("OARO_ChangeOffset_Reformation".Translate(reformation.label, offset.ToStringWithSign("0.##"))
                                                                      .Colorize((offset < 0f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green));
            }
        }
    }
}