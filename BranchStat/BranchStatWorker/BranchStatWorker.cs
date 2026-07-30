using OberoniaAurea.RatkinOrder.Utility;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatWorker(BranchStatDef statDef) : StatWorker<BranchStatDef, Branch, BranchStatRequestData>(statDef)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override float GetNewStatValue(BranchStatRequestData requestData, float? baseValueOverride)
    {
        return requestData.GetNewStatValue(baseValueOverride);
    }

    public override bool TransformModify(BranchStatRequestData requestData,
                                         out StatTransformer transformer,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        transformer = StatTransformer.Invalid;

        bool hasTransformer = false;

        if (requestData.Target.TransformerHandler.TryGetStatTransformer(StatDef, out StatTransformer tempTransformer))
        {
            hasTransformer = true;
            transformer.MergeWith(tempTransformer);
            if (!resultOnly)
            {
                explanation.AppendLine("OARO_StatExplain_BranchInfrastructure".Translate().Colorize(Color.cyan));
                tempTransformer.AppendTransToExplanation(StatDef, explanation);
            }
        }

        if (requestData.Target.RatkinOrder.TransformerHandler.TryGetStatTransformer(StatDef, out tempTransformer))
        {
            hasTransformer = true;
            transformer.MergeWith(tempTransformer);
            if (!resultOnly)
            {
                explanation.AppendLine("OARO_StatExplain_OrderInfrastructure".Translate().Colorize(Color.cyan));
                tempTransformer.AppendTransToExplanation(StatDef, explanation);
            }
        }

        return hasTransformer;
    }

    public override bool PartPostTransModify(BranchStatRequestData requestData,
                                             ref StatComputeState curValue,
                                             bool resultOnly = true,
                                             StringBuilder explanation = null)
    {
        if (StatDef.statParts is not null)
        {
            List<BranchStatPart> parts = StatDef.statParts;
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