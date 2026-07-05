using RimWorld;
using System;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_BranchEffectTagOffset : BranchStatPart
{
    [MustTranslate]
    public string reasonOverride;

    public string effectTag;
    public float offset;

    public override void PostTransform(Branch branch, ref float curValue)
    {
        if (branch.EffectTags.HasTag(effectTag))
        {
            curValue += offset;
        }
    }

    public override void ModifyExplanation(Branch branch, BranchStatDef statDef, StringBuilder explanation)
    {
        if (branch.EffectTags.HasTag(effectTag))
        {
            explanation.Append(ExplanatCap);
            Color color = (offset < 0f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green;
            string offsetArg = statDef.statType == BranchStatDef.StatType.Percent ? offset.ToStringPercentSigned("0.##") : offset.ToStringWithSign("0.##");

            if (String.IsNullOrEmpty(reasonOverride))
            {
                explanation.AppendLine("OARO_ChangeOffset_BranchEffectTag".Translate(effectTag.Named(KeyLibrary_FormatArgName.EffectTag), offsetArg.Named(KeyLibrary_FormatArgName.Offset))
                                                                          .Colorize(color));
            }
            else
            {
                explanation.AppendLine(reasonOverride.Formatted(effectTag.Named(KeyLibrary_FormatArgName.EffectTag), offsetArg.Named(KeyLibrary_FormatArgName.Offset))
                                                     .Colorize(color));
            }
        }
    }
}