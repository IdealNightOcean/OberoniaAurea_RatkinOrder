using OberoniaAurea_Frame;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatPart_Factor_KnightEffectTag : ResidentKnightStatPart
{
    [NoTranslate]
    public string effectTag;
    public float factor = 1f;

    public override bool PostTransModify(ResidentKnightStatRequestData requestData,
                                         ref StatComputeState curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        if (!requestData.Target.EffectTags.HasTag(effectTag))
            return false;

        curValue.Value *= factor;
        if (!resultOnly)
        {
            explanation.AppendLineWithSeparator(
                text: "OARO_ChangeFactor_PawnEffectTag"
                .Translate(KeyLibrary_EffectTag.StudyElite.Named(OARO_KeyLibrary_FormatArgName.EffectTag),
                           OARO_StatExplanationUtility.FactorNamedArgument(factor, requestData.StatDef))
                .ColorizeStrByFactor(factor, reverse: requestData.StatDef.reverse),
                separator: KeyLibrary_Misc.SpaceCap4);
        }

        return true;
    }
}