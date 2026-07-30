using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatPart_Offset_KnightEffectTag : ResidentKnightStatPart
{
    [NoTranslate]
    public string effectTag;
    public float offset;


    public override bool PostTransModify(ResidentKnightStatRequestData requestData,
                                         ref StatComputeState curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        if (!requestData.Target.EffectTags.HasTag(effectTag))
            return false;

        curValue.Value += offset;
        if (!resultOnly)
        {
            explanation.AppendLineWithSeparator(
                text: "OARO_ChangeOffset_PawnEffectTag"
                .Translate(KeyLibrary_EffectTag.StudyElite.Named(OARO_KeyLibrary_FormatArgName.EffectTag),
                           OARO_StatExplanationUtility.OffsetNamedArgument(offset, requestData.StatDef))
                .ColorizeStrByOffset(offset, reverse: requestData.StatDef.reverse),
                separator: KeyLibrary_Misc.SpaceCap4);
        }

        return true;
    }
}
