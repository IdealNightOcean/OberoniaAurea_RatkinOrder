using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class OARO_StatExplanationUtility
{
    public static string GetBaseValueExplanation(this OAROStatDefBase statDef, float baseValue, string format = "0.##")
    {
        return statDef.statType switch
        {
            BranchStatDef.StatType.Int => (string)"OARO_StatExplain_BaseValue".Translate(((int)baseValue).ToStringWithSign()),
            BranchStatDef.StatType.Float => (string)"OARO_StatExplain_BaseValue".Translate(baseValue.ToStringWithSign(format)),
            BranchStatDef.StatType.Percent => (string)"OARO_StatExplain_BaseValue".Translate(baseValue.ToStringPercent(format)),
            _ => KeyLibrary_Misc.ErrorTipWithColor,
        };
    }

    public static void AppendStatResultExplanation(StringBuilder modifyExplain,
                                                   OAROStatDefBase statDef,
                                                   float baseValue,
                                                   float finalValue)
    {
        modifyExplain.AppendLine();
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
    public static NamedArgument OffsetNamedArgument(float offset, OAROStatDefBase statDef, string format = "0.##")
    {
        return statDef.statType == BranchStatDef.StatType.Percent ?
            OAFrame_TextUtility.PercentNamedArgument(offset, KeyLibrary_FormatArgName.Offset, format: format, includeSign: true) :
            OAFrame_TextUtility.FloatNamedArgument(offset, KeyLibrary_FormatArgName.Offset, format: format, includeSign: true);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NamedArgument ColoredOffsetNamedArgument(float offset, OAROStatDefBase statDef, string format = "0.##", float originPoint = 0f)
    {
        return statDef.statType == BranchStatDef.StatType.Percent ?
            OAFrame_TextUtility.ColoredPercentNamedArgument(offset, KeyLibrary_FormatArgName.Offset, format: format, includeSign: true, originPoint: originPoint, reverse: statDef.reverse) :
            OAFrame_TextUtility.ColoredFloatNamedArgument(offset, KeyLibrary_FormatArgName.Offset, format: format, includeSign: true, originPoint: originPoint, reverse: statDef.reverse);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NamedArgument FactorNamedArgument(float factor, OAROStatDefBase statDef, string format = "0.##")
    {
        return statDef.statType == BranchStatDef.StatType.Percent ?
            OAFrame_TextUtility.PercentNamedArgument(factor, KeyLibrary_FormatArgName.Factor, format: format, includeSign: false) :
            OAFrame_TextUtility.FloatNamedArgument(factor, KeyLibrary_FormatArgName.Factor, format: format, includeSign: false);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NamedArgument ColoredFactorNamedArgument(float factor, OAROStatDefBase statDef, string format = "0.##", float originPoint = 1f)
    {
        return statDef.statType == BranchStatDef.StatType.Percent ?
            OAFrame_TextUtility.ColoredPercentNamedArgument(factor, KeyLibrary_FormatArgName.Factor, format: format, includeSign: false, originPoint: originPoint, reverse: statDef.reverse) :
            OAFrame_TextUtility.ColoredFloatNamedArgument(factor, KeyLibrary_FormatArgName.Factor, format: format, includeSign: false, originPoint: originPoint, reverse: statDef.reverse);
    }
}


public static class OARO_StatUtility
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetStatValue(this Branch target, BranchStatDef statDef, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        return statDef.Worker.GetValue(new BranchStatRequestData(target, statDef), baseValueOverride, immediateUpdate);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetStatValue(this ResidentKnight knight, ResidentKnightStatDef statDef, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        return statDef.Worker.GetValue(new ResidentKnightStatRequestData(knight, statDef), baseValueOverride, immediateUpdate);
    }
    private static float GetStatValue<TTarget, TDef>(TTarget target, StatWorker<TDef, TTarget, StatRequestData<TDef, TTarget>> statWorker, TDef statDef, float? baseValueOverride = null, bool immediateUpdate = false) where TDef : OAROStatDefBase
    {
        return statWorker.GetValue(new StatRequestData<TDef, TTarget>(target, statDef), baseValueOverride, immediateUpdate);
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetStatValue(this BranchStatDef statDef, BranchStatRequestData requestData, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        requestData.StatDef = statDef;
        return statDef.Worker.GetValue(requestData, baseValueOverride, immediateUpdate);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetStatValue(this ResidentKnightStatDef statDef, ResidentKnightStatRequestData requestData, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        requestData.StatDef = statDef;
        return statDef.Worker.GetValue(requestData, baseValueOverride, immediateUpdate);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetStatValue(this BranchStatRequestData requestData, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        return requestData.StatDef.Worker.GetValue(requestData, baseValueOverride, immediateUpdate);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetStatValue(this ResidentKnightStatRequestData requestData, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        return requestData.StatDef.Worker.GetValue(requestData, baseValueOverride, immediateUpdate);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetStatValue<TRequestData, TDef, TTarget>(TRequestData requestData, TDef statDef, StatWorker<TDef, TTarget, TRequestData> statWorker, float? baseValueOverride = null, bool immediateUpdate = false)
        where TRequestData : StatRequestData<TDef, TTarget>
        where TDef : OAROStatDefBase
    {
        requestData.StatDef = statDef;
        return statWorker.GetValue(requestData, baseValueOverride, immediateUpdate);
    }




    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetNewStatValue(this BranchStatRequestData requestData, float? baseValueOverride = null)
    {
        return GetNewStatValue<BranchStatRequestData, BranchStatDef, Branch>(requestData, requestData.StatDef.Worker, baseValueOverride);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetNewStatValue(this ResidentKnightStatRequestData requestData, float? baseValueOverride = null)
    {
        return GetNewStatValue<ResidentKnightStatRequestData, ResidentKnightStatDef, ResidentKnight>(requestData, requestData.StatDef.Worker, baseValueOverride);
    }

    private static float GetNewStatValue<TRequestData, TDef, TTarget>(TRequestData requestData, StatWorker<TDef, TTarget, TRequestData> statWorker, float? baseValueOverride = null)
        where TRequestData : StatRequestData<TDef, TTarget>
        where TDef : OAROStatDefBase
    {
        float result;

        try
        {
            TDef statDef = requestData.StatDef;

            statWorker.PrepareInitialBaseValue(requestData, baseValueOverride);
            float curValue = requestData.BaseValue;

            if (statWorker.TransformModify(requestData, out StatTransformer transformer))
            {
                curValue = transformer.DoTransform(statDef, curValue);
            }

            statWorker.PostTransModify(requestData, ref curValue);
            statWorker.PartPostTransModify(requestData, ref curValue);

            result = Mathf.Clamp(curValue, statDef.minValue, statDef.maxValue);
            if (statDef.statType == BranchStatDef.StatType.Int)
            {
                result = Mathf.Round(result);
            }

        }
        catch (Exception ex)
        {
            result = baseValueOverride ?? requestData?.StatDef?.baseValue ?? 0f;
            ModUtility.LogExceptionError(ex,
                errorDesc: $"计算新的ResidentKnightStat值: [ResidentKnightStat: {requestData?.StatDef?.label}, ResidentKnightId: {requestData.Target?.ToString()}]",
                typeName: nameof(OARO_StatUtility),
                methodName: nameof(GetNewStatValue),
                needStackTrace: true);
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (StringBuilder explanationBuilder, float? resultNullable) GetStatModifyExplanation(this BranchStatRequestData requestData,
                                                                                                                                  float? baseValueOverride = null,
                                                                                                                                  bool showResultValue = true)
    {
        return GetStatModifyExplanation<BranchStatRequestData, BranchStatDef, Branch>(requestData, requestData.StatDef.Worker, baseValueOverride, showResultValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (StringBuilder explanationBuilder, float? resultNullable) GetStatModifyExplanation(this ResidentKnightStatRequestData requestData,
                                                                                                                                  float? baseValueOverride = null,
                                                                                                                                  bool showResultValue = true)
    {
        return GetStatModifyExplanation<ResidentKnightStatRequestData, ResidentKnightStatDef, ResidentKnight>(requestData, requestData.StatDef.Worker, baseValueOverride, showResultValue);
    }
    private static (StringBuilder explanationBuilder, float? resultNullable) GetStatModifyExplanation<TRequestData, TDef, TTarget>(TRequestData requestData,
        StatWorker<TDef, TTarget, TRequestData> statWorker,
                                                                                                                                  float? baseValueOverride = null,
                                                                                                                                  bool showResultValue = true)
        where TRequestData : StatRequestData<TDef, TTarget>
        where TDef : OAROStatDefBase


    {
        if (requestData is null || requestData.StatDef is null)
            return (new StringBuilder(KeyLibrary_Misc.ErrorTipWithColor), null);

        StringBuilder explanationBuilder = new(256);
        try
        {
            TDef statDef = requestData.StatDef;
            statWorker.PrepareInitialBaseValue(requestData: requestData,
                                               baseValueOverride: baseValueOverride,
                                               resultOnly: false,
                                               explanation: explanationBuilder);
            float baseValue = requestData.BaseValue;
            float curValue = baseValue;

            explanationBuilder.AppendLine("OARO_StatExplain_BranchInfrastructure".Translate());
            if (statWorker.TransformModify(requestData, out StatTransformer transformer, resultOnly: false, explanation: explanationBuilder))
            {
                curValue = transformer.DoTransform(statDef, curValue);
            }
            else
            {
                explanationBuilder.AppendLineWithSeparator(KeyLibrary_Misc.SpaceCap4, "None".Translate());
            }

            explanationBuilder.AppendLine("OARO_StatExplain_PostTransModify".Translate().Colorize(Color.cyan));

            bool hasPostTransModify = statWorker.PostTransModify(requestData: requestData,
                                                                 curValue: ref curValue,
                                                                 resultOnly: false,
                                                                 explanation: explanationBuilder);

            bool hasPartPostTransModify = statWorker.PartPostTransModify(requestData: requestData,
                                                                         curValue: ref curValue,
                                                                         resultOnly: false,
                                                                         explanation: explanationBuilder);

            if (!hasPostTransModify && !hasPartPostTransModify)
            {
                explanationBuilder.AppendLineWithSeparator(KeyLibrary_Misc.SpaceCap4, "None".Translate());
            }

            if (showResultValue)
            {
                float result = Mathf.Clamp(curValue, statDef.minValue, statDef.maxValue);
                if (statDef.statType == BranchStatDef.StatType.Int)
                {
                    result = Mathf.Round(result);
                }
                statWorker.UpdateStatCache(requestData.Target, result);
                OARO_StatExplanationUtility.AppendStatResultExplanation(explanationBuilder, statDef, baseValue, result);
                return (explanationBuilder, result);
            }
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"生成ResidentKnightStat修改说明: [ResidentKnightStat: {requestData.StatDef?.label}, ResidentKnightId: {requestData.Target?.ToString()}]",
                typeName: nameof(OARO_StatUtility),
                methodName: nameof(GetStatModifyExplanation),
                needStackTrace: true);

            explanationBuilder = new(KeyLibrary_Misc.ErrorTipWithColor);
        }

        return (explanationBuilder, null);
    }

}