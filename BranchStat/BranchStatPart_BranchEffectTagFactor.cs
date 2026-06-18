using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_BranchEffectTagFactor : BranchStatPart
{
    [MustTranslate]
    public string reasonOverride;

    public string effectTag;
    public float factor;

    public override void PostTransform(Branch branch, ref float curValue)
    {
        if (branch.EffectTags.HasTag(effectTag))
        {
            curValue *= factor;
        }
    }

    public override void ModifyExplanation(Branch branch, BranchStatDef statDef, StringBuilder explanation)
    {
        if (branch.EffectTags.HasTag(effectTag))
        {
            explanation.Append(ExplanatCap);
            Color color = (factor < 1f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green;
            string factorArg = statDef.statType == BranchStatDef.StatType.Percent ? factor.ToStringPercentSigned("0.##") : factor.ToStringWithSign("0.##");

            if (string.IsNullOrEmpty(reasonOverride))
            {
                explanation.AppendLine("OARO_ChangeFactor_BranchEffectTag".Translate(effectTag.Named(KeyLibrary_FormatArgName.EffectTag), factorArg.Named(KeyLibrary_FormatArgName.Factor))
                                                                          .Colorize(color));
            }
            else
            {
                explanation.AppendLine(reasonOverride.Formatted(effectTag.Named(KeyLibrary_FormatArgName.EffectTag), factorArg.Named(KeyLibrary_FormatArgName.Factor))
                                                     .Colorize(color));
            }
        }
    }
}