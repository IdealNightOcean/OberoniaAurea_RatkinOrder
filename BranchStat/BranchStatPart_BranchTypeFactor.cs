using System.Text;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.Branch;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_BranchTypeFactor : BranchStatPart
{
    public BranchType branchType;
    public float factor;

    public override void PostTransform(Branch branch, ref float curValue)
    {
        if (branch.IsBranchOfType(branchType))
        {
            curValue *= factor;
        }
    }

    public override void ModifyExplanation(Branch branch, BranchStatDef statDef, StringBuilder explanation)
    {
        if (branch.IsBranchOfType(branchType))
        {
            explanation.Append("    ");
            if (statDef.statType == BranchStatDef.StatType.Percent)
            {
                explanation.AppendLine("OARO_ChangeFactor_BranchTypeOf".Translate($"OARO_BranchType_{branchType}".Translate(), factor.ToStringPercentSigned("0.##"))
                                                                       .Colorize((factor < 1f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green));
            }
            else
            {
                explanation.AppendLine("OARO_ChangeFactor_BranchTypeOf".Translate($"OARO_BranchType_{branchType}".Translate(), factor.ToStringWithSign("0.##"))
                                                                       .Colorize((factor < 1f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green));
            }
        }
    }
}
