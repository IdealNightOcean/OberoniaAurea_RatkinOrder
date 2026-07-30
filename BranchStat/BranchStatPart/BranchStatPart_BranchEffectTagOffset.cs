using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_BranchEffectTagOffset : BranchStatPart
{
    [MustTranslate]
    public string reasonOverride;

    public string effectTag;
    public float offset;


    public override bool PostTransModify(BranchStatRequestData requestData,
                                         ref StatComputeState curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        if (!requestData.Target.EffectTags.HasTag(effectTag))
            return false;

        curValue.Value += offset;

        if (!resultOnly)
        {
            NamedArgument offsetArg = OARO_StatExplanationUtility.ColoredOffsetNamedArgument(offset, requestData.StatDef);

            if (String.IsNullOrEmpty(reasonOverride))
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_ChangeOffset_BranchEffectTag".Translate(
                        effectTag.Named(OARO_KeyLibrary_FormatArgName.EffectTag),
                        offsetArg).ColorizeStrByOffset(offset, reverse: requestData.StatDef.reverse),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
            else
            {
                explanation.AppendLineWithSeparator(
                    text: reasonOverride.Formatted(
                        effectTag.Named(OARO_KeyLibrary_FormatArgName.EffectTag),
                        offsetArg).ColorizeStrByOffset(offset, reverse: requestData.StatDef.reverse),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        return true;
    }
}