using System;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class ResidentKnightStatUtility
{
    public static string GetStatModifyExplanationStr(ResidentKnightStatRequestData requestData, ResidentKnightStatDef statDef, float? baseValueOverride = null, bool showResultValue = true)
    {
        return GetStatModifyExplanation(requestData, statDef, baseValueOverride, showResultValue).ToString();
    }

    public static StringBuilder GetStatModifyExplanation(ResidentKnightStatRequestData requestData, ResidentKnightStatDef statDef, float? baseValueOverride = null, bool showResultValue = true)
    {
        if (requestData is null || statDef is null)
            return new StringBuilder(string.Empty);

        StringBuilder explanation = new(256);
        try
        {
            float baseValue = statDef.Worker.PrepareInitialBaseValeExplanation(explanation, requestData, baseValueOverride);

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

            float curValue = (showResultValue && hasTrans) ? transformer.DoTransform(statDef, baseValue) : baseValue;

            statDef.Worker.PostTransModifyExplanation(requestData: requestData,
                                                      statDef: statDef,
                                                      baseValue: baseValue,
                                                      curValue: curValue,
                                                      explanation: explanation,
                                                      showResultValue: showResultValue);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"生成ResidentKnightStat修改说明: [ResidentKnightStat: {statDef?.label}, ResidentKnightId: {requestData.Knight?.GetUniqueLoadID()}]",
                typeName: nameof(ResidentKnightStatUtility),
                methodName: nameof(GetStatModifyExplanation),
                needStackTrace: true);
            explanation = new("ERROR (；′⌒`)".Colorize(ColorLibrary.RedReadable));
        }

        return explanation;
    }

    public static void AppendStatResultExplanation(StringBuilder modifyExplain, ResidentKnightStatDef statDef, float finalValue, float? baseValueOverride = null)
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
    public static float GetStatValue(this ResidentKnight knight, ResidentKnightStatDef statDef, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        return statDef.Worker.GetValue(new ResidentKnightStatRequestData(knight, statDef), baseValueOverride, immediateUpdate);
    }

    public static float GetNewStatValue(ResidentKnightStatRequestData requestData, ResidentKnightStatDef statDef, float? baseValueOverride = null)
    {
        float result;
        try
        {
            result = statDef.Worker.PrepareInitialBaseValue(requestData, baseValueOverride);

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

        }
        catch (Exception ex)
        {
            result = baseValueOverride ?? statDef?.baseValue ?? 0f;
            ModUtility.LogExceptionError(ex,
                errorDesc: $"计算新的ResidentKnightStat值: [ResidentKnightStat: {statDef?.label}, ResidentKnightId: {requestData.Knight?.GetUniqueLoadID()}]",
                typeName: nameof(ResidentKnightStatUtility),
                methodName: nameof(GetNewStatValue),
                needStackTrace: true);
        }

        return result;
    }

    public static float GetNewStatValueFormTrans(ResidentKnightStatRequestData requestData, ResidentKnightStatDef statDef, StatTransformer transformer, float? baseValueOverride = null)
    {
        float result;
        try
        {
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
            result = baseValueOverride ?? statDef?.baseValue ?? 0f;
            ModUtility.LogExceptionError(ex,
                errorDesc: $"计算新的BranchStat值: [BranchStat: {statDef?.label}, BranchId: {requestData.Knight?.GetUniqueLoadID()}]",
                typeName: nameof(BranchStatUtility),
                methodName: nameof(GetNewStatValueFormTrans),
                needStackTrace: true);
        }
        return result;
    }
}