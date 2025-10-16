using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class BranchStatUtility
{
    public static StringBuilder GetBranchStatModifyExplanation(this Branch branch, BranchStatDef statDef, float? baseValueOverride = null, bool updateCache = true)
    {
        StringBuilder explanation = new();
        BranchStatTransformer transformer = BranchStatTransformer.DefaultTransformer;
        bool hasTrans = false;
        if (branch.RatkinOrder.TransformerHandler.TryGetStatTransformer(statDef, out BranchStatTransformer tempTransformer))
        {
            explanation.AppendLine("OARO_StatExplain_BranchInfrastructure".Translate());
            tempTransformer.ModifyExplanation(statDef, explanation);
            hasTrans = true;
            transformer.MergeWith(tempTransformer);
        }
        if (branch.TransformerHandler.TryGetStatTransformer(statDef, out transformer))
        {
            explanation.AppendLine("OARO_StatExplain_OrderReformation".Translate());
            tempTransformer.ModifyExplanation(statDef, explanation);
            hasTrans = true;
            transformer.MergeWith(tempTransformer);
        }

        float baseValue = baseValueOverride ?? statDef.baseValue;
        float result = baseValue;
        if (hasTrans)
        {
            result = transformer.DoTransform(statDef, result);
        }

        if (statDef.statParts is not null)
        {
            explanation.AppendLine("OARO_StatExplain_StatParts".Translate());
            List<BranchStatPart> statParts = statDef.statParts;
            for (int i = 0; i < statParts.Count; i++)
            {
                result = statParts[i].PostTransform(branch, result);
                statParts[i].ModifyExplanation(branch, explanation);
            }
        }

        if (updateCache)
        {
            statDef.Worker.UpdateStatCache(branch, result);
        }

        explanation.AppendLine();
        switch (statDef.statType)
        {
            case BranchStatDef.StatType.Int:
                explanation.AppendLine("OARO_StatExplain_ResultInt".Translate(Mathf.Round(result).ToStringWithSign())
                                                                .Colorize((result < baseValue ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green));
                break;
            case BranchStatDef.StatType.Float:
                explanation.AppendLine("OARO_StatExplain_Result".Translate(result.ToStringWithSign("F2"))
                                                                .Colorize((result < baseValue ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green));
                break;
            case BranchStatDef.StatType.Percent:
                explanation.AppendLine("OARO_StatExplain_Result".Translate(result.ToStringPercentSigned("F2"))
                                                                .Colorize((result < baseValue ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green));
                break;
            default: break;
        }

        return explanation;
    }

    public static bool TryGetStatTransformer(this Branch branch, BranchStatDef statDef, out BranchStatTransformer transformer)
    {
        transformer = BranchStatTransformer.DefaultTransformer;
        bool hasTransformer = false;
        if (branch.RatkinOrder.TransformerHandler.TryGetStatTransformer(statDef, out BranchStatTransformer tempTransformer))
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
}