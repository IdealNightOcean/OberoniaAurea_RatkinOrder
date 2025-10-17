using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_DailyPopulationGrowth : BranchStatPart
{
    public override void PostTransform(Branch branch, ref float curValue)
    {
        if (branch.RatkinOrder.Funds < 0.5f)
        {
            curValue *= Mathf.Max(0.01f, 1f - (0.5f - branch.RatkinOrder.Funds) * 2f);
        }

        float populationRatio = branch.PopulationHandler.PopulationRatio;
        if (populationRatio > 1f)
        {
            curValue -= ((populationRatio - 1f) * 0.1f * branch.PopulationHandler.Population);
        }
    }

    public override void ModifyExplanation(Branch branch, StringBuilder explanation)
    {
        float change;
        if (branch.RatkinOrder.Funds < 0.5f)
        {
            change = Mathf.Max(0.01f, 1f - (0.5f - branch.RatkinOrder.Funds) * 2f);
            explanation.Append("    ");
            explanation.AppendLine("OARO_ChangeFactor_Fund".Translate(change.ToString("F2")).Colorize(ColorLibrary.RedReadable));
        }
        float populationRatio = branch.PopulationHandler.PopulationRatio;
        if (populationRatio > 1f)
        {
            change = -((populationRatio - 1f) * 0.1f * branch.PopulationHandler.Population);
            explanation.Append("    ");
            explanation.AppendLine("OARO_ChangeOffset_BranchPopulation".Translate(change.ToStringWithSign("F2")).Colorize(ColorLibrary.RedReadable));
        }
    }
}