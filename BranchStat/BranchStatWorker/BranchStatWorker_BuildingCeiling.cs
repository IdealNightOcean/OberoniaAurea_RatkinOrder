using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatWorker_BuildingCeiling(BranchStatDef statDef) : BranchStatWorker(statDef)
{
    public override bool PostTransModify(BranchStatRequestData requestData,
                                         ref StatComputeState curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        Branch branch = requestData.Target;
        bool hasModification = false;
        int offset = branch.FacilityHandler.TotalFacilityLevel / 8;
        if (offset != 0)
        {
            hasModification = true;
            curValue.Value += offset;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_ChangeOffset_FacilityLevel"
                    .Translate(OARO_StatExplanationUtility.OffsetNamedArgument(offset, StatDef))
                    .ColorizeStrByOffset(offset, reverse: StatDef.reverse),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        offset = Mathf.Min(branch.PopulationHandler.Population / 2000, 2);
        if (offset != 0)
        {
            hasModification = true;
            curValue.Value += offset;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_ChangeOffset_BranchPopulation".Translate(OAFrame_TextUtility.ColoredIntNamedArgument(offset, KeyLibrary_FormatArgName.Offset, includeSign: true, reverse: StatDef.reverse)),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        return hasModification;
    }
}