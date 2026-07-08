using OberoniaAurea_Frame;
using RimWorld;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatWorker_DailyPopulationGrowth(BranchStatDef statDef) : BranchStatWorker(statDef)
{
    public override bool PostTransModify(BranchStatRequestData requestData,
                                         ref float curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        Branch branch = requestData.Target;
        bool hasModified = false;

        if (branch.RatkinOrder.Funds < 0.5f)
        {
            float factor = Mathf.Max(0.01f, 1f - (0.5f - branch.RatkinOrder.Funds) * 2f);
            if (factor != 1f)
            {
                hasModified = true;
                curValue *= factor;
                if (!resultOnly)
                {
                    explanation.AppendLineWithSeparator(
                        text: "OARO_ChangeFactor_Fund"
                        .Translate(OARO_StatExplanationUtility.FactorNamedArgument(factor, requestData.StatDef))
                        .ColorizeStrByFactor(factor, reverse: requestData.StatDef.reverse),
                        separator: KeyLibrary_Misc.SpaceCap4);
                }
            }

        }

        if (branch.PopulationHandler.HasContractBuff)
        {
            hasModified = true;
            float factor = 1.25f;
            curValue *= factor;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_ChangeFactor_ContractBuff".Translate(OAFrame_TextUtility.ColoredFloatNamedArgument(factor, KeyLibrary_FormatArgName.Factor, originPoint: 1f, reverse: StatDef.reverse)),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        float offset = -((branch.PopulationHandler.PopulationRatio - 1f) * 0.1f * branch.PopulationHandler.Population);
        if (offset != 0f)
        {
            hasModified = true;
            curValue += offset;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_ChangeOffset_BranchPopulation".Translate(OAFrame_TextUtility.ColoredFloatNamedArgument(offset, KeyLibrary_FormatArgName.Offset, includeSign: true, reverse: StatDef.reverse)),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }


        return hasModified;
    }
}