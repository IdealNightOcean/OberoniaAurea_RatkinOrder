using OberoniaAurea_Frame;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatWorker(ResidentKnightStatDef statDef)
{
    public readonly ResidentKnightStatDef StatDef = statDef ?? throw new ArgumentNullException(nameof(statDef));

    private readonly Dictionary<ResidentKnight, CacheEnty> temporaryStatCache = statDef.cacheable ? new(8) : null;

    public virtual void PrepareInitialBaseValue(ResidentKnightStatRequestData requestData,
                                                float? baseValueOverride = null,
                                                bool resultOnly = true,
                                                StringBuilder explanation = null)
    {
        float baseValue = baseValueOverride ?? StatDef.baseValue;
        requestData.BaseValue = baseValue;
        if (!resultOnly)
        {
            explanation.AppendLine(GetBaseValueExplanation(baseValue));
        }
    }


    public virtual void PostTransModify(ResidentKnightStatRequestData requestData,
                                        ref float curValue,
                                        bool resultOnly = true,
                                        StringBuilder explanation = null)
    { }

    public float GetValue(ResidentKnightStatRequestData requestData, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        if (!StatDef.cacheable)
            return ResidentKnightStatUtility.GetNewStatValue(requestData, baseValueOverride);

        int ticksGame = Find.TickManager.TicksGame;
        ResidentKnight knight = requestData.Knight;
        if (temporaryStatCache.TryGetValue(knight, out CacheEnty cacheEnty))
        {
            if (immediateUpdate || cacheEnty.ExpiredTick < ticksGame)
            {
                CacheEnty newEnty = BuildNewCacheEnty(requestData, baseValueOverride, ticksGame);
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
            CacheEnty newEnty = BuildNewCacheEnty(requestData, baseValueOverride, ticksGame);
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

    protected string GetBaseValueExplanation(float baseValue)
    {
        return StatDef.statType switch
        {
            BranchStatDef.StatType.Int => (string)"OARO_StatExplain_BaseValue".Translate(((int)baseValue).ToStringWithSign()),
            BranchStatDef.StatType.Float => (string)"OARO_StatExplain_BaseValue".Translate(baseValue.ToStringWithSign("0.##")),
            BranchStatDef.StatType.Percent => (string)"OARO_StatExplain_BaseValue".Translate(baseValue.ToStringPercent("0.##")),
            _ => KeyLibrary_Misc.ErrorTipWithColor,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static CacheEnty BuildNewCacheEnty(ResidentKnightStatRequestData requestData, float? baseValueOverride, int ticksGame)
    {
        float result = ResidentKnightStatUtility.GetNewStatValue(requestData, baseValueOverride);
        return new CacheEnty(result, ticksGame + requestData.StatDef.cacheDuration);
    }
}