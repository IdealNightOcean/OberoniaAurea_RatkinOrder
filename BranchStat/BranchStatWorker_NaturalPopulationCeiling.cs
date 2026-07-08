using OberoniaAurea_Frame;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatWorker_NaturalPopulationCeiling(BranchStatDef statDef) : BranchStatWorker(statDef)
{
    public override bool PostTransModify(BranchStatRequestData requestData,
                                         ref float curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        int offset = requestData.Target.FacilityHandler.TotalFacilityLevel * 200;
        curValue += offset;

        if (!resultOnly)
        {
            explanation.AppendLineWithSeparator(
                    text: "OARO_ChangeOffset_FacilityLevel"
                    .Translate(OARO_StatExplanationUtility.OffsetNamedArgument(offset, requestData.StatDef))
                    .ColorizeStrByOffset(offset, reverse: requestData.StatDef.reverse),
                    separator: KeyLibrary_Misc.SpaceCap4);
        }

        return true;
    }
}