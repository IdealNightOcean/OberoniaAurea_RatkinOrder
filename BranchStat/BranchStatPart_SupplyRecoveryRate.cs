using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_SupplyRecoveryRate : BranchStatPart
{
    public override void PostTransform(Branch branch, ref float curValue)
    {
        curValue += branch.PopulationHandler.Population / 100f * 0.0005f;
    }

    public override void ModifyExplanation(Branch branch, StringBuilder explanation)
    {
        explanation.Append("    ");
        explanation.AppendLine("OARO_StatOffset_BranchPopulation".Translate((branch.PopulationHandler.Population / 100f * 0.0005f).ToStringPercent("F2"))
                                                                 .Colorize(Color.green));
    }
}