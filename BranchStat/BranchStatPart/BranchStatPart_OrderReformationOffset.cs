using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using RimWorld;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_OrderReformationOffset : BranchStatPart
{
    public OrderReformationDef reformation;
    public float offset;

    public override bool PostTransModify(BranchStatRequestData requestData,
                                         ref StatComputeState curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        if (!requestData.Target.RatkinOrder.ReformationManager.HasReformation(reformation))
            return false;

        curValue.Value += offset;

        if (!resultOnly)
        {
            explanation.AppendLineWithSeparator(
                text: "OARO_ChangeOffset_Reformation"
                .Translate(reformation.Named(KeyLibrary_FormatArgName.DEF),
                           OARO_StatExplanationUtility.OffsetNamedArgument(offset, requestData.StatDef))
                .ColorizeStrByOffset(offset, reverse: requestData.StatDef.reverse),
                separator: KeyLibrary_Misc.SpaceCap4);
        }

        return true;
    }
}