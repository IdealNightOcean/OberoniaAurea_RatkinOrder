using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using OberoniaAurea_Frame.DataLibrary;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_OrderReformationFactor : BranchStatPart
{
    public OrderReformationDef reformation;
    public float factor;

    public override bool PostTransModify(BranchStatRequestData requestData,
                                         ref StatComputeState curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        if (!requestData.Target.RatkinOrder.ReformationManager.HasReformation(reformation))
            return false;

        curValue.Value *= factor;

        if (!resultOnly)
        {
            explanation.AppendLineWithSeparator(
                text: "OARO_ChangeFactor_Reformation"
                .Translate(
                    reformation.Named(KeyLibrary_FormatArgName.DEF),
                    OARO_StatExplanationUtility.FactorNamedArgument(factor, requestData.StatDef))
                .ColorizeStrByFactor(factor, reverse: requestData.StatDef.reverse),
                separator: KeyLibrary_Misc.SpaceCap4);
        }

        return true;
    }
}