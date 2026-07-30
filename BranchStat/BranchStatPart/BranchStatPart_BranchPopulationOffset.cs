using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using RimWorld;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_BranchPopulationOffset : BranchStatPart
{
    public float unitBase;
    public float unitScale;

    public override bool PostTransModify(BranchStatRequestData requestData,
                                         ref StatComputeState curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        float offset = requestData.Target.PopulationHandler.Population / unitBase * unitScale;
        if (offset == 0f)
            return false;

        curValue.Value += offset;

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