using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_BranchEffectTagFactor : BranchStatPart
{
    [MustTranslate]
    public string reasonOverride;

    public string effectTag;
    public float factor;

    public override bool PostTransModify(BranchStatRequestData requestData,
                                         ref StatComputeState curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        if (!requestData.Target.EffectTags.HasTag(effectTag))
            return false;

        curValue.Value *= factor;

        if (!resultOnly)
        {

            NamedArgument factorArg = OARO_StatExplanationUtility.FactorNamedArgument(factor, requestData.StatDef);

            if (String.IsNullOrEmpty(reasonOverride))
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_ChangeFactor_BranchEffectTag".Translate(
                        effectTag.Named(OARO_KeyLibrary_FormatArgName.EffectTag),
                        factorArg).ColorizeStrByFactor(factor, reverse: requestData.StatDef.reverse),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
            else
            {
                explanation.AppendLineWithSeparator(
                    text: reasonOverride.Formatted(
                        effectTag.Named(OARO_KeyLibrary_FormatArgName.EffectTag),
                        factorArg).ColorizeStrByFactor(factor, reverse: requestData.StatDef.reverse),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        return true;
    }
}