using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;


public class ResidentKnightStatWorker(ResidentKnightStatDef statDef) : StatWorker<ResidentKnightStatDef, ResidentKnight, ResidentKnightStatRequestData>(statDef)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override float GetNewStatValue(ResidentKnightStatRequestData requestData, float? baseValueOverride)
    {
        return requestData.GetStatValue(baseValueOverride);
    }

    public override bool TransformModify(ResidentKnightStatRequestData requestData,
                                         out StatTransformer transformer,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        transformer = StatTransformer.Invalid;

        if (requestData.Target.VirtueHandler.TransformerHandler.TryGetStatTransformer(StatDef, out StatTransformer tempTransformer))
        {
            transformer.MergeWith(tempTransformer);
            if (!resultOnly)
            {
                explanation.AppendLine("OARO_StatExplain_ResidentKnightVirtue".Translate().Colorize(Color.cyan));
                tempTransformer.AppendTransToExplanation(StatDef, explanation);
            }

            return true;
        }

        return false;
    }

    public override bool PartPostTransModify(ResidentKnightStatRequestData requestData,
                                             ref StatComputeState curValue,
                                             bool resultOnly = true,
                                             StringBuilder explanation = null)
    {
        if (StatDef.statParts is not null)
        {
            List<ResidentKnightStatPart> parts = StatDef.statParts;
            bool hasModified = false;
            for (int i = 0; i < parts.Count; i++)
            {
                bool partModified = parts[i].PostTransModify(requestData: requestData, curValue: ref curValue);
                hasModified = hasModified || partModified;
            }

            return hasModified;
        }
        return false;
    }
}