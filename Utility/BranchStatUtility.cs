using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class BranchStatUtility
{
    public static string GetStatModifyExplanationStr(Branch branch, BranchStatDef statDef, float? baseValueOverride = null, bool showResultValue = true)
    {
        return GetStatModifyExplanation(branch, statDef, baseValueOverride, showResultValue).ToString();
    }

    public static StringBuilder GetStatModifyExplanation(Branch branch, BranchStatDef statDef, float? baseValueOverride = null, bool showResultValue = true)
    {
        if (branch is null || statDef is null)
        {
            return new StringBuilder(string.Empty);
        }
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

            BranchStatTransformer transformer = new();
            bool hasTrans = false;

            if (branch.RatkinOrder.TransformerHandler.TryGetStatTransformer(statDef, out BranchStatTransformer tempTransformer))
            {
                hasTrans = true;
                explanation.AppendLine("OARO_StatExplain_OrderReformation".Translate());
                tempTransformer.AppendTransToExplanation(statDef, explanation);

                if (showResultValue)
                {
                    transformer.MergeWith(tempTransformer);
                }
            }
            if (branch.TransformerHandler.TryGetStatTransformer(statDef, out tempTransformer))
            {
                hasTrans = true;
                explanation.AppendLine("OARO_StatExplain_BranchInfrastructure".Translate());
                tempTransformer.AppendTransToExplanation(statDef, explanation);

                if (showResultValue)
                {
                    transformer.MergeWith(tempTransformer);
                }
            }

            float result = (showResultValue && hasTrans) ? transformer.DoTransform(statDef, baseValue) : baseValue;

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
                result = Mathf.Clamp(result, statDef.minValue, statDef.maxValue);
                if (statDef.statType == BranchStatDef.StatType.Int)
                {
                    result = Mathf.Round(result);
                }
                statDef.Worker.UpdateStatCache(branch, result);
                AppendStatResultExplanation(explanation, statDef, result, baseValue);
            }
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"generating BranchStat modification explanation: [BranchStat: {statDef?.label}, BranchId: {branch?.GetUniqueLoadID()}]",
                typeName: nameof(BranchStatUtility),
                methodName: nameof(GetStatModifyExplanation),
                needStackTrace: true);
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
            BranchStatTransformer transformer = new();
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
            ModUtility.LogExceptionError(ex,
                errorDesc: $"calculating new BranchStat value: [BranchStat: {statDef?.label}, BranchId: {branch?.GetUniqueLoadID()}]",
                typeName: nameof(BranchStatUtility),
                methodName: nameof(GetNewStatValue),
                needStackTrace: true);
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
            ModUtility.LogExceptionError(ex,
                errorDesc: $"calculating new BranchStat value: [BranchStat: {statDef?.label}, BranchId: {branch?.GetUniqueLoadID()}]",
                typeName: nameof(BranchStatUtility),
                methodName: nameof(GetNewStatValueFormTrans),
                needStackTrace: true);
        }
        return result;
    }
}