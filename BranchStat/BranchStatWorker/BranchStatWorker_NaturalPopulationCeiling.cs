using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatWorker_NaturalPopulationCeiling(BranchStatDef statDef) : BranchStatWorker(statDef)
{
    public override bool PostTransModify(BranchStatRequestData requestData,
                                         ref StatComputeState curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        int offset = requestData.Target.FacilityHandler.TotalFacilityLevel * 200;
        curValue.Value += offset;

        if (!resultOnly)
        {
            explanation.AppendLineWithSeparator(
                    text: "OARO_ChangeOffset_FacilityLevel"
                    .Translate(OARO_StatExplanationUtility.OffsetNamedArgument(offset, StatDef))
                    .ColorizeStrByOffset(offset, reverse: StatDef.reverse),
                    separator: KeyLibrary_Misc.SpaceCap4);
        }

        return true;
    }
}