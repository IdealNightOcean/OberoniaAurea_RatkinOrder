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

    public virtual bool PrepareInitialBaseValue(ResidentKnightStatRequestData requestData,
                                                float? baseValueOverride = null,
                                                bool resultOnly = true,
                                                StringBuilder explanation = null)
    {
        float baseValue = baseValueOverride ?? StatDef.baseValue;
        requestData.BaseValue = baseValue;
        if (!resultOnly)
        {
            explanation.AppendLine(StatDef.GetBaseValueExplanation(baseValue));
        }
        return true;
    }


    public virtual bool PostTransModify(ResidentKnightStatRequestData requestData,
                                        ref float curValue,
                                        bool resultOnly = true,
                                        StringBuilder explanation = null)
    { return true; }

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



    protected static bool TryCastRequestData<T>(ResidentKnightStatRequestData requestData,
                                                out T targetRequestData,
                                                bool resultOnly = true,
                                                StringBuilder explanation = null) where T : ResidentKnightStatRequestData
    {
        targetRequestData = null;
        if (requestData is T target)
        {
            targetRequestData = target;
            return true;
        }

        Log.Error($"RequestData 不是 {nameof(T)} 类型。实际类型：{requestData.GetType().FullName}");
        if (!resultOnly)
            explanation.AppendLine(KeyLibrary_Misc.ErrorTipWithColor);

        return false;

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static CacheEnty BuildNewCacheEnty(ResidentKnightStatRequestData requestData, float? baseValueOverride, int ticksGame)
    {
        float result = ResidentKnightStatUtility.GetNewStatValue(requestData, baseValueOverride);
        return new CacheEnty(result, ticksGame + requestData.StatDef.cacheDuration);
    }
}