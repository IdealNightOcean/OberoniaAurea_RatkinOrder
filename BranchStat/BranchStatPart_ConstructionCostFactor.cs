using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_ConstructionCostFactor : BranchStatPart
{
    public override void PostTransform(Branch branch, ref float curValue)
    {
        curValue -= Mathf.Clamp(branch.PopulationHandler.Population / 100f * 0.01f, 0f, 0.9f);
        curValue = curValue < 0f ? 0f : curValue;
    }

    public override void ModifyExplanation(Branch branch, BranchStatDef statDef, StringBuilder explanation)
    {
        float costRateReduce = -Mathf.Clamp(branch.PopulationHandler.Population / 100f * 0.01f, 0f, 0.9f);
        explanation.Append("    ");
        explanation.AppendLine("OARO_ChangeFactor_BranchPopulation".Translate(costRateReduce.ToString("0.##"))
                                                                   .Colorize(Color.green));
    }
}