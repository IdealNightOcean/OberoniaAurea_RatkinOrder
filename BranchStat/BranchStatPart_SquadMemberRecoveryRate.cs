using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_SquadMemberRecoveryRate : BranchStatPart
{
    public override float PostTransform(Branch branch, float value)
    {
        return value + branch.PopulationHandler.Population / 100f * 0.005f;
    }
    public override void ModifyExplanation(Branch branch, StringBuilder explanation)
    {
        explanation.Append("    ");
        explanation.AppendLine("OARO_StatOffset_BranchPopulation".Translate((branch.PopulationHandler.Population / 100f * 0.005f).ToStringWithSign("F2"))
                                                                 .Colorize(Color.green));
    }
}