using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class BranchStatUtility
{
    public static StringBuilder GetStatModifyExplanation(Branch branch, BranchStatDef statDef, float? baseValueOverride = null, bool showResultValue = true)
    {
        StringBuilder explanation = new(256);
        try
        {
            float baseValue = baseValueOverride ?? statDef.baseValue;
            switch (statDef.statType)
            {
                case BranchStatDef.StatType.Int:
                    explanation.AppendLine("OARO_StatExplain_BaseValue".Translate(((int)baseValue).ToStringWithSign()));
                    break;
                case BranchStatDef.StatType.Float:
                    explanation.AppendLine("OARO_StatExplain_BaseValue".Translate(baseValue.ToStringWithSign("0.##")));
                    break;
                case BranchStatDef.StatType.Percent:
                    explanation.AppendLine("OARO_StatExplain_BaseValue".Translate(baseValue.ToStringPercent("0.##")));
                    break;
                default: break;
            }

            BranchStatTransformer transformer = BranchStatTransformer.DefaultTransformer;
            bool hasTrans = false;

            if (branch.RatkinOrder.TransformerHandler.TryGetStatTransformer(statDef, out BranchStatTransformer tempTransformer))
            {
                explanation.AppendLine("OARO_StatExplain_BranchInfrastructure".Translate());
                tempTransformer.AppendTransExplanation(statDef, explanation);

                if (showResultValue)
                {
                    transformer.MergeWith(tempTransformer);
                    hasTrans = true;
                }
            }
            if (branch.TransformerHandler.TryGetStatTransformer(statDef, out transformer))
            {
                explanation.AppendLine("OARO_StatExplain_OrderReformation".Translate());
                tempTransformer.AppendTransExplanation(statDef, explanation);

                if (showResultValue)
                {
                    transformer.MergeWith(tempTransformer);
                    hasTrans = true;
                }
            }

            float result = baseValue;
            if (showResultValue && hasTrans)
            {
                result = transformer.DoTransform(statDef, result);
            }

            List<BranchStatPart> statParts = statDef.statParts;
            if (statParts is not null)
            {
                explanation.AppendLine("OARO_StatExplain_StatParts".Translate());
                if (showResultValue)
                {
                    for (int i = 0; i < statParts.Count; i++)
                    {
                        statParts[i].PostTransform(branch, ref result);
                        statParts[i].ModifyExplanation(branch, explanation);
                    }
                }
                else
                {
                    for (int i = 0; i < statParts.Count; i++)
                    {
                        statParts[i].ModifyExplanation(branch, explanation);
                    }
                }
            }

            if (showResultValue)
            {
                statDef.Worker.UpdateStatCache(branch, result);
                AppendStatResultExplanation(explanation, statDef, result, baseValue);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to generate BranchStat modification explanation: [BranchStat: {statDef?.label}, BranchId: {branch?.GetUniqueLoadID()}]\nException:\n" + ex);
            explanation = new("ERROR (；′⌒`)".Colorize(ColorLibrary.RedReadable));
        }

        return explanation;
    }

    public static void AppendStatResultExplanation(StringBuilder modifyExplain, BranchStatDef statDef, float finalValue, float? baseValueOverride = null)
    {
        modifyExplain.AppendLine();
        float baseValue = baseValueOverride ?? statDef.baseValue;
        switch (statDef.statType)
        {
            case BranchStatDef.StatType.Int:
                modifyExplain.AppendLine("OARO_StatExplain_ResultInt".Translate(Mathf.Round(finalValue).ToString())
                                                                     .Colorize((finalValue < baseValue ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green));
                break;
            case BranchStatDef.StatType.Float:
                modifyExplain.AppendLine("OARO_StatExplain_Result".Translate(finalValue.ToString("0.##"))
                                                                  .Colorize((finalValue < baseValue ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green));
                break;
            case BranchStatDef.StatType.Percent:
                modifyExplain.AppendLine("OARO_StatExplain_Result".Translate(finalValue.ToStringPercent("0.##"))
                                                                  .Colorize((finalValue < baseValue ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green));
                break;
            default: break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetStatValue(this Branch branch, BranchStatDef statDef, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        return statDef.Worker.GetValue(branch, baseValueOverride, immediateUpdate);
    }

    public static float GetNewStatValue(this Branch branch, BranchStatDef statDef, float? baseValueOverride = null)
    {
        float result;
        try
        {
            BranchStatTransformer transformer = BranchStatTransformer.DefaultTransformer;
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
            if (hasTransformer)
            {
                result = transformer.DoTransform(statDef, baseValueOverride);
            }
            else
            {
                result = baseValueOverride ?? statDef.baseValue;
            }

            if (statDef.statParts is not null)
            {
                for (int i = 0; i < statDef.statParts.Count; i++)
                {
                    statDef.statParts[i].PostTransform(branch, ref result);
                }
            }

            result = Mathf.Clamp(result, statDef.minValue, statDef.maxValue);
            if (statDef.statType == BranchStatDef.StatType.Int)
            {
                result = Mathf.Round(result);
            }
        }
        catch (Exception ex)
        {
            result = baseValueOverride ?? statDef?.baseValue ?? 0f;
            Log.Error($"Failed to calculate new BranchStat value: [BranchStat: {statDef?.label}, BranchId: {branch?.GetUniqueLoadID()}].\nException:\n" + ex);
        }

        return result;
    }

    public static float GetNewStatValueFormTrans(this Branch branch, BranchStatDef statDef, BranchStatTransformer transformer, float? baseValueOverride = null)
    {
        float result;
        try
        {
            result = transformer.DoTransform(statDef, baseValueOverride);
            if (statDef.statParts is not null)
            {
                for (int i = 0; i < statDef.statParts.Count; i++)
                {
                    statDef.statParts[i].PostTransform(branch, ref result);
                }
            }

            result = Mathf.Clamp(result, statDef.minValue, statDef.maxValue);
            if (statDef.statType == BranchStatDef.StatType.Int)
            {
                result = Mathf.Round(result);
            }
        }
        catch (Exception ex)
        {
            result = baseValueOverride ?? statDef?.baseValue ?? 0f;
            Log.Error($"Failed to calculate new BranchStat value: [BranchStat: {statDef?.label}, BranchId: {branch?.GetUniqueLoadID()}].\nException:\n" + ex);
        }
        return result;
    }
}