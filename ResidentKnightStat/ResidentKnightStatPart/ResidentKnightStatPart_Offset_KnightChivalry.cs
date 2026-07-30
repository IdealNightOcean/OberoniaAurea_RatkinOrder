using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatPart_Offset_KnightChivalry : ResidentKnightStatPart
{
    public float offset;
    public KnightChivalryDef chivalryDef;

    public override bool PostTransModify(ResidentKnightStatRequestData requestData,
                                         ref StatComputeState curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        if (chivalryDef is null || chivalryDef != requestData.Target.Chivalry)
            return false;

        curValue.Value += offset;
        if (!resultOnly)
        {
            explanation.AppendLineWithSeparator(
                text: "OARO_ChangeOffset_KnightHasChivalry"
                .Translate(
                    chivalryDef.Named(OARO_KeyLibrary_FormatArgName.CHIVALRY),
                    OARO_StatExplanationUtility.OffsetNamedArgument(offset, requestData.StatDef))
                .ColorizeStrByOffset(offset, reverse: requestData.StatDef.reverse),
                separator: KeyLibrary_Misc.SpaceCap4);
        }

        return true;

    }
}
