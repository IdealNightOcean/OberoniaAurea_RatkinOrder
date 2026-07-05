using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatWorker(ResidentKnightStatDef statDef)
{
    public readonly ResidentKnightStatDef StatDef = statDef ?? throw new ArgumentNullException(nameof(statDef));

    private readonly Dictionary<ResidentKnight, CacheEnty> temporaryStatCache = statDef.cacheable ? new(8) : null;

    public virtual float PrepareInitialBaseValue(ResidentKnightStatRequestData requestData, float? baseValueOverride = null)
    {
        return baseValueOverride ?? StatDef.baseValue;
    }

    public virtual float PrepareInitialBaseValeExplanation(StringBuilder explanation, ResidentKnightStatRequestData requestData, float? baseValueOverride = null)
    {
        float baseValue = baseValueOverride ?? StatDef.baseValue;
        switch (StatDef.statType)
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
        return baseValue;
    }

    public virtual void PostTransModify(ResidentKnightStatRequestData requestData, ref float curValue)
    {
        List<ResidentKnightStatPart> statParts = StatDef.statParts;
        if (statParts is not null)
        {
            for (int i = 0; i < statParts.Count; i++)
            {
                statParts[i].PostTransModify(requestData, ref curValue);
            }
        }

        curValue = Mathf.Clamp(curValue, statDef.minValue, statDef.maxValue);
        if (statDef.statType == BranchStatDef.StatType.Int)
        {
            curValue = Mathf.Round(curValue);
        }
    }

    public virtual void PostTransModifyExplanation(ResidentKnightStatRequestData requestData,
                                                    ResidentKnightStatDef statDef,
                                                    float baseValue,
                                                    float curValue,
                                                    StringBuilder explanation,
                                                    bool showResultValue = true)
    {
        List<ResidentKnightStatPart> statParts = statDef.statParts;
        if (statParts is not null)
        {
            explanation.AppendLine("OARO_StatExplain_StatParts".Translate());
            if (showResultValue)
            {
                for (int i = 0; i < statParts.Count; i++)
                {
                    statParts[i].PostTransModify(requestData, ref curValue);
                    statParts[i].PostTransModifyExplanation(requestData, statDef, explanation);
                }
            }
            else
            {
                for (int i = 0; i < statParts.Count; i++)
                {
                    statParts[i].PostTransModifyExplanation(requestData, statDef, explanation);
                }
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
            ResidentKnightStatUtility.AppendStatResultExplanation(explanation, statDef, result, baseValue);
        }
    }

    public float GetValue(ResidentKnightStatRequestData requestData, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        if (!StatDef.cacheable)
            return ResidentKnightStatUtility.GetNewStatValue(requestData, StatDef, baseValueOverride);

        int ticksGame = Find.TickManager.TicksGame;
        ResidentKnight knight = requestData.Knight;
        if (temporaryStatCache.TryGetValue(knight, out CacheEnty cacheEnty))
        {
            if (immediateUpdate || cacheEnty.ExpiredTick < ticksGame)
            {
                CacheEnty newEnty = BuildNewCacheEnty(StatDef, requestData, baseValueOverride, ticksGame);
                temporaryStatCache[knight] = newEnty;
                return newEnty.CacheValue;
            }
            else
            {
                return cacheEnty.CacheValue;
            }
        }
        else
        {
            CacheEnty newEnty = BuildNewCacheEnty(StatDef, requestData, baseValueOverride, ticksGame);
            temporaryStatCache.Add(knight, newEnty);
            return newEnty.CacheValue;
        }
    }

    public void UpdateStatCache(ResidentKnight knight, float value)
    {
        if (StatDef.cacheable)
        {
            temporaryStatCache[knight] = new CacheEnty(value, Find.TickManager.TicksGame + StatDef.cacheDuration);
        }
    }

    public void DeleteStatCache() => temporaryStatCache.Clear();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static CacheEnty BuildNewCacheEnty(ResidentKnightStatDef statDef, ResidentKnightStatRequestData requestData, float? baseValueOverride, int ticksGame)
    {
        float result = ResidentKnightStatUtility.GetNewStatValue(requestData, statDef, baseValueOverride);
        return new CacheEnty(result, ticksGame + statDef.cacheDuration);
    }
}