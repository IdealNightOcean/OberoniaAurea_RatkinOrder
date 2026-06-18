using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_AffectRadius : BranchStatPart
{
    public override void PostTransform(Branch branch, ref float curValue)
    {
        curValue += branch.RatkinOrder.FundHandler.Funds / 0.08f;
    }

    public override void ModifyExplanation(Branch branch, BranchStatDef statDef, StringBuilder explanation)
    {
        explanation.Append(ExplanatCap);
        explanation.AppendLine("OARO_ChangeOffset_Fund".Translate((branch.RatkinOrder.FundHandler.Funds / 0.08f).ToStringWithSign("0.##"))
                                                       .Colorize(Color.green));
    }
}