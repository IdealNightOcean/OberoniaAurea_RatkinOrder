using OberoniaAurea_Frame;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatWorker_ConstructionCostFactor(BranchStatDef statDef) : BranchStatWorker(statDef)
{
    public override bool PostTransModify(BranchStatRequestData requestData,
                                         ref float curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        float offset = -Mathf.Clamp(requestData.Target.PopulationHandler.Population / 100f * 0.01f, 0f, 0.9f);
        if (offset == 0f)
            return false;

        curValue += offset;
        if (!resultOnly)
        {
            explanation.AppendLineWithSeparator(
                text: "OARO_ChangeOffset_BranchPopulation"
                .Translate(OARO_StatExplanationUtility.OffsetNamedArgument(offset, requestData.StatDef))
                .ColorizeStrByOffset(offset, reverse: requestData.StatDef.reverse),
                separator: KeyLibrary_Misc.SpaceCap4);
        }
        return true;
    }
}