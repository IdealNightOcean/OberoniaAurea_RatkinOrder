using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_ConstructionCost : BranchStatPart
{
    public override void PostTransform(Branch branch, ref float curValue)
    {
        float costRate = 1f - Mathf.Clamp(branch.PopulationHandler.Population / 100f * 0.01f, 0f, 0.9f);
        curValue *= costRate;
    }

    public override void ModifyExplanation(Branch branch, StringBuilder explanation)
    {
        float costRate = 1f - Mathf.Clamp(branch.PopulationHandler.Population / 100f * 0.01f, 0f, 0.9f);
        explanation.Append("    ");
        explanation.AppendLine("OARO_StatFactor_BranchPopulation".Translate(costRate.ToString("F2"))
                                                                 .Colorize(Color.green));
    }
}