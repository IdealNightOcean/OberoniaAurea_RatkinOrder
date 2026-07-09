using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

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

            StatComputeState curValue = new();
            statWorker.PrepareInitialBaseValue(requestData, ref curValue, baseValueOverride);
            float baseValue = curValue.Value;

            if (!curValue.IsConverged)
            {
                if (statWorker.TransformModify(requestData, out StatTransformer transformer))
                {
                    curValue.Value = transformer.DoTransform(statDef, curValue.Value);
                }
            }

            if (!curValue.IsConverged)
            {
                statWorker.PostTransModify(requestData, ref curValue);
            }

            if (!curValue.IsConverged)
            {
                statWorker.PartPostTransModify(requestData, ref curValue);
            }

            result = Mathf.Clamp(curValue.Value, statDef.minValue, statDef.maxValue);
            if (statDef.statType == BranchStatDef.StatType.Integer)
            {
                result = Mathf.Round(result);
            }

        }
        catch (Exception ex)
        {
            result = baseValueOverride ?? requestData?.StatDef?.baseValue ?? 0f;
            ModUtility.LogExceptionError(ex,
                errorDesc: $"计算新的Stat值: [Stat: {requestData?.StatDef?.label}, Target: {requestData.Target?.ToString()}]",
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

            StatComputeState curValue = new();
            statWorker.PrepareInitialBaseValue(requestData: requestData,
                                               curValue: ref curValue,
                                               baseValueOverride: baseValueOverride,
                                               resultOnly: false,
                                               explanation: explanationBuilder);

            float baseValue = curValue.Value;
            if (!curValue.IsConverged)
            {
                explanationBuilder.AppendLine("OARO_StatExplain_BranchInfrastructure".Translate());
                if (statWorker.TransformModify(requestData,
                                               out StatTransformer transformer,
                                               resultOnly: false,
                                               explanation: explanationBuilder))
                {
                    curValue.Value = transformer.DoTransform(statDef, curValue.Value);
                }
                else
                {
                    explanationBuilder.AppendLineWithSeparator(text: "None".Translate(), separator: KeyLibrary_Misc.SpaceCap4);
                }
            }

            if (!curValue.IsConverged)
            {
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
                    explanationBuilder.AppendLineWithSeparator(text: "None".Translate(), separator: KeyLibrary_Misc.SpaceCap4);
                }
            }

            if (showResultValue)
            {
                float result = Mathf.Clamp(curValue.Value, statDef.minValue, statDef.maxValue);
                if (statDef.statType == BranchStatDef.StatType.Integer)
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
                errorDesc: $"生成Stat修正说明: [Stat: {requestData.StatDef?.label}, Target: {requestData.Target?.ToString()}]",
                typeName: nameof(OARO_StatUtility),
                methodName: nameof(GetStatModifyExplanation),
                needStackTrace: true);

            explanationBuilder = new(KeyLibrary_Misc.ErrorTipWithColor);
        }

        return (explanationBuilder, null);
    }

}