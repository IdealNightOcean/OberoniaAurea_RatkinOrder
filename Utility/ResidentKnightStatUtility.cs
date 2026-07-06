using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class ResidentKnightStatUtility
{
    public static string GetBaseValueExplanation(this ResidentKnightStatDef statDef, float baseValue, string format = "0.##")
    {
        return statDef.statType switch
        {
            BranchStatDef.StatType.Int => (string)"OARO_StatExplain_BaseValue".Translate(((int)baseValue).ToStringWithSign()),
            BranchStatDef.StatType.Float => (string)"OARO_StatExplain_BaseValue".Translate(baseValue.ToStringWithSign(format)),
            BranchStatDef.StatType.Percent => (string)"OARO_StatExplain_BaseValue".Translate(baseValue.ToStringPercent(format)),
            _ => KeyLibrary_Misc.ErrorTipWithColor,
        };
    }

    public static string GetStatModifyExplanationStr(ResidentKnightStatRequestData requestData, float? baseValueOverride = null, bool showResultValue = true)
    {
        return GetStatModifyExplanation(requestData, baseValueOverride, showResultValue).ToString();
    }

    public static (StringBuilder, float?) GetStatModifyExplanation(ResidentKnightStatRequestData requestData, float? baseValueOverride = null, bool showResultValue = true)
    {
        if (requestData is null || requestData.StatDef is null)
            return (new StringBuilder(KeyLibrary_Misc.ErrorTipWithColor), null);

        StringBuilder explanation = new(256);
        try
        {
            ResidentKnightStatDef statDef = requestData.StatDef;
            statDef.Worker.PrepareInitialBaseValue(requestData: requestData,
                                                   baseValueOverride: baseValueOverride,
                                                   resultOnly: false,
                                                   explanation: explanation);

            float curValue = requestData.BaseValue;

            StatTransformer transformer = new();
            bool hasTrans = false;

            if (requestData.Knight.TransformerHandler.TryGetStatTransformer(statDef, out StatTransformer tempTransformer))
            {
                hasTrans = true;
                explanation.AppendLine("OARO_StatExplain_BranchInfrastructure".Translate());
                tempTransformer.AppendTransToExplanation(statDef, explanation);

                if (showResultValue)
                {
                    transformer.MergeWith(tempTransformer);
                }
            }

            curValue = (showResultValue && hasTrans) ? transformer.DoTransform(statDef, curValue) : curValue;

            explanation.AppendLine("OARO_StatExplain_PostTransModify".Translate().Colorize(Color.cyan));

            statDef.Worker.PostTransModify(requestData: requestData,
                                          curValue: ref curValue,
                                          resultOnly: false,
                                          explanation: explanation);

            List<ResidentKnightStatPart> statParts = statDef.statParts;
            if (statParts is not null)
            {
                for (int i = 0; i < statParts.Count; i++)
                {
                    statParts[i].PostTransModify(requestData: requestData,
                                                 curValue: ref curValue,
                                                 resultOnly: false,
                                                 explanation: explanation);
                }
            }

            if (showResultValue)
            {
                float result = Mathf.Clamp(curValue, statDef.minValue, statDef.maxValue);
                if (statDef.statType == BranchStatDef.StatType.Int)
                {
                    result = Mathf.Round(result);
                }
                statDef.Worker.UpdateStatCache(requestData.Knight, result);
                ResidentKnightStatUtility.AppendStatResultExplanation(explanation, requestData, result);
                return (explanation, result);
            }
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"生成ResidentKnightStat修改说明: [ResidentKnightStat: {requestData.StatDef?.label}, ResidentKnightId: {requestData.Knight?.GetUniqueLoadID()}]",
                typeName: nameof(ResidentKnightStatUtility),
                methodName: nameof(GetStatModifyExplanation),
                needStackTrace: true);
            explanation = new(KeyLibrary_Misc.ErrorTipWithColor);
        }

        return (explanation, null);
    }

    public static void AppendStatResultExplanation(StringBuilder modifyExplain, ResidentKnightStatRequestData requestData, float finalValue)
    {
        modifyExplain.AppendLine();
        ResidentKnightStatDef statDef = requestData.StatDef;
        float baseValue = requestData.BaseValue;
        switch (statDef.statType)
        {
            case BranchStatDef.StatType.Int:
                modifyExplain.AppendLine("OARO_StatExplain_ResultInt".Translate(OAFrame_TextUtility.ColoredFloatString(finalValue, format: "F0", originPoint: baseValue, reverse: statDef.reverse)));
                break;
            case BranchStatDef.StatType.Float:
                modifyExplain.AppendLine("OARO_StatExplain_Result".Translate(OAFrame_TextUtility.ColoredFloatString(finalValue, originPoint: baseValue, reverse: statDef.reverse)));
                break;
            case BranchStatDef.StatType.Percent:
                modifyExplain.AppendLine("OARO_StatExplain_Result".Translate(OAFrame_TextUtility.ColoredPercentString(finalValue, originPoint: baseValue, reverse: statDef.reverse)));
                break;
            default: break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetStatValue(this ResidentKnight knight, ResidentKnightStatDef statDef, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        return statDef.Worker.GetValue(new ResidentKnightStatRequestData(knight, statDef), baseValueOverride, immediateUpdate);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetStatValue(this ResidentKnightStatDef statDef, ResidentKnightStatRequestData requestData, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        requestData.StatDef = statDef;
        return statDef.Worker.GetValue(requestData, baseValueOverride, immediateUpdate);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetStatValue(this ResidentKnightStatRequestData requestData, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        return requestData.StatDef.Worker.GetValue(requestData, baseValueOverride, immediateUpdate);
    }

    public static float GetNewStatValue(ResidentKnightStatRequestData requestData, float? baseValueOverride = null)
    {
        float result;

        try
        {
            ResidentKnightStatDef statDef = requestData.StatDef;
            statDef.Worker.PrepareInitialBaseValue(requestData, baseValueOverride);
            result = requestData.BaseValue;

            StatTransformer transformer = new();
            bool hasTransformer = false;
            if (requestData.Knight.TransformerHandler.TryGetStatTransformer(statDef, out StatTransformer tempTransformer))
            {
                transformer.MergeWith(tempTransformer);
                hasTransformer = true;
            }
            if (hasTransformer)
            {
                result = transformer.DoTransform(statDef, result);
            }

            statDef.Worker.PostTransModify(requestData, ref result);

            result = Mathf.Clamp(result, statDef.minValue, statDef.maxValue);
            if (statDef.statType == BranchStatDef.StatType.Int)
            {
                result = Mathf.Round(result);
            }

        }
        catch (Exception ex)
        {
            result = baseValueOverride ?? requestData?.StatDef?.baseValue ?? 0f;
            ModUtility.LogExceptionError(ex,
                errorDesc: $"计算新的ResidentKnightStat值: [ResidentKnightStat: {requestData?.StatDef?.label}, ResidentKnightId: {requestData.Knight?.GetUniqueLoadID()}]",
                typeName: nameof(ResidentKnightStatUtility),
                methodName: nameof(GetNewStatValue),
                needStackTrace: true);
        }

        return result;
    }

    public static float GetNewStatValueFormTrans(ResidentKnightStatRequestData requestData, StatTransformer transformer, float? baseValueOverride = null)
    {
        if (requestData is null || requestData.StatDef is null)
        {
            Log.Error("[OARO] 试图获取无效的 ResidentKnightStat 值, ");
            return 0f;
        }

        float result;
        try
        {
            ResidentKnightStatDef statDef = requestData.StatDef;
            result = transformer.DoTransform(statDef, baseValueOverride);
            if (statDef.statParts is not null)
            {
                for (int i = 0; i < statDef.statParts.Count; i++)
                {
                    statDef.statParts[i].PostTransModify(requestData, ref result);
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
            result = baseValueOverride ?? requestData.StatDef.baseValue;
            ModUtility.LogExceptionError(ex,
                errorDesc: $"计算新的BranchStat值: [BranchStat: {requestData.StatDef.label}, BranchId: {requestData.Knight?.GetUniqueLoadID()}]",
                typeName: nameof(BranchStatUtility),
                methodName: nameof(GetNewStatValueFormTrans),
                needStackTrace: true);
        }
        return result;
    }
}