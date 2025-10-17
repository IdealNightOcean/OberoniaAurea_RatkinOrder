using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_SquadMemberCeiling : BranchStatPart
{
    public override void PostTransform(Branch branch, ref float curValue)
    {
        curValue += branch.RatkinOrder.FundHandler.Funds;
    }

    public override void ModifyExplanation(Branch branch, StringBuilder explanation)
    {
        explanation.AppendLine("OARO_StatOffset_Fund".Translate((branch.RatkinOrder.FundHandler.Funds / 0.08f).ToStringWithSign("F2"))
                                                     .Colorize(Color.green));
    }
}