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
        if (branch.PopulationHandler.HasContractBuff)
        {
            curValue *= 1.25f;
        }

        float populationRatio = branch.PopulationHandler.PopulationRatio;
        if (populationRatio > 1f)
        {
            curValue -= ((populationRatio - 1f) * 0.1f * branch.PopulationHandler.Population);
        }
    }

    public override void ModifyExplanation(Branch branch, BranchStatDef statDef, StringBuilder explanation)
    {
        float change;
        if (branch.RatkinOrder.Funds < 0.5f)
        {
            change = Mathf.Max(0.01f, 1f - (0.5f - branch.RatkinOrder.Funds) * 2f);
            explanation.Append(ExplanatCap);
            explanation.AppendLine("OARO_ChangeFactor_Fund".Translate(change.ToString("0.##")).Colorize(ColorLibrary.RedReadable));
        }
        if (branch.PopulationHandler.HasContractBuff)
        {
            explanation.Append(ExplanatCap);
            explanation.AppendLine("OARO_ChangeFactor_ContractBuff".Translate(1.25f.ToStringPercent("0.##")).Colorize(Color.green));
        }
        float populationRatio = branch.PopulationHandler.PopulationRatio;
        if (populationRatio > 1f)
        {
            change = -((populationRatio - 1f) * 0.1f * branch.PopulationHandler.Population);
            explanation.Append(ExplanatCap);
            explanation.AppendLine("OARO_ChangeOffset_BranchPopulation".Translate(change.ToStringWithSign("0.##")).Colorize(ColorLibrary.RedReadable));
        }
    }
}