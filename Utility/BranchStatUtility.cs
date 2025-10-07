using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class BranchStatUtility
{
    public static bool TryGetStatTransformer(this Branch branch, BranchStatDef statDef, out BranchStatTransformer transformer)
    {
        transformer = BranchStatTransformer.DefaultTransformer;
        bool hasTransformer = false;
        if (branch.RatkinOrder.ReformationManager.TransformerHandler.TryGetStatTransformer(statDef, out BranchStatTransformer tempTransformer))
        {
            transformer.MergeWith(tempTransformer);
            hasTransformer = true;
        }
        if (branch.TransformerHandler.TryGetStatTransformer(statDef, out tempTransformer))
        {
            transformer.MergeWith(tempTransformer);
            hasTransformer = true;
        }
        return hasTransformer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetStatValue(this Branch branch, BranchStatDef statDef, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        return statDef.Worker.GetValue(branch, baseValueOverride, immediateUpdate);
    }

    public static float GetNewStatValue(this Branch branch, BranchStatDef statDef, float? baseValueOverride = null)
    {
        float result;
        if (TryGetStatTransformer(branch, statDef, out BranchStatTransformer transformer))
        {
            result = transformer.DoTransform(statDef, baseValueOverride);
        }
        else
        {
            result = baseValueOverride ?? statDef.baseValue;
        }

        if (statDef.statParts is not null)
        {
            foreach (BranchStatPart part in statDef.statParts)
            {
                result = part.PostTransform(branch, result);
            }
        }

        result = Mathf.Clamp(result, statDef.minValue, statDef.maxValue);
        if (statDef.statType == BranchStatDef.StatType.Int)
        {
            result = Mathf.Round(result);
        }
        return result;
    }

    public static float GetNewStatValueFormTrans(this Branch branch, BranchStatDef statDef, BranchStatTransformer transformer, float? baseValueOverride = null)
    {
        float result = transformer.DoTransform(statDef, baseValueOverride);

        if (statDef.statParts is not null)
        {
            foreach (BranchStatPart part in statDef.statParts)
            {
                result = part.PostTransform(branch, result);
            }
        }

        result = Mathf.Clamp(result, statDef.minValue, statDef.maxValue);
        if (statDef.statType == BranchStatDef.StatType.Int)
        {
            result = Mathf.Round(result);
        }
        return result;
    }

    public static StringBuilder GetStatExplanation(this Branch branch, BranchStatDef statDef, float? baseValueOverride = null)
    {
        StringBuilder sb = new("OARO_StatBaseValue".Translate(baseValueOverride ?? statDef.baseValue));
        if (branch.RatkinOrder.ReformationManager.TransformerHandler.TryGetStatTransformer(statDef, out BranchStatTransformer tempTransformer))
        {
            sb.AppendInNewLine("OARO_ReformationTransform".Translate());
            sb.AppendInNewLine(tempTransformer.GetTransformExplanation());
        }
        if (branch.TransformerHandler.TryGetStatTransformer(statDef, out tempTransformer))
        {
            sb.AppendInNewLine("OARO_BranchTransform".Translate());
            sb.AppendInNewLine(tempTransformer.GetTransformExplanation());
        }

        return sb;
    }
}
