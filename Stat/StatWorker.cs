using OberoniaAurea_Frame;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class StatWorker<TDef, TTarget, TRequestData>(TDef statDef)
    where TDef : OAROStatDefBase
    where TRequestData : StatRequestData<TDef, TTarget>
{
    public readonly TDef StatDef = statDef ?? throw new ArgumentNullException(nameof(statDef));

    private readonly Dictionary<TTarget, CacheEnty> temporaryStatCache = statDef.cacheable ? new(8) : null;

    public virtual bool PrepareInitialBaseValue(TRequestData requestData,
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

    public virtual bool TransformModify(TRequestData requestData,
                                        out StatTransformer transformer,
                                        bool resultOnly = true,
                                        StringBuilder explanation = null)
    {
        transformer = StatTransformer.Invalid;
        return false;
    }

    public virtual bool PostTransModify(TRequestData requestData,
                                        ref float curValue,
                                        bool resultOnly = true,
                                        StringBuilder explanation = null)
    { return false; }

    public virtual bool PartPostTransModify(TRequestData requestData,
                                        ref float curValue,
                                        bool resultOnly = true,
                                        StringBuilder explanation = null)
    {
        return false;
    }

    public float GetValue(TRequestData requestData, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        if (!StatDef.cacheable)
            return GetNewStatValue(requestData, baseValueOverride);

        int ticksGame = Find.TickManager.TicksGame;
        TTarget target = requestData.Target;
        if (temporaryStatCache.TryGetValue(target, out CacheEnty cacheEnty))
        {
            if (immediateUpdate || cacheEnty.ExpiredTick < ticksGame)
            {
                CacheEnty newEnty = BuildNewCacheEnty(requestData, baseValueOverride, ticksGame);
                temporaryStatCache[target] = newEnty;
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
            temporaryStatCache.Add(target, newEnty);
            return newEnty.CacheValue;
        }
    }

    public void UpdateStatCache(TTarget target, float value)
    {
        if (StatDef.cacheable)
        {
            temporaryStatCache[target] = new CacheEnty(value, Find.TickManager.TicksGame + StatDef.cacheDuration);
        }
    }

    public void DeleteStatCache() => temporaryStatCache.Clear();

    protected static bool TryCastRequestData<T>(TRequestData requestData,
                                                out T targetRequestData,
                                                bool resultOnly = true,
                                                StringBuilder explanation = null) where T : TRequestData
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
    private CacheEnty BuildNewCacheEnty(TRequestData requestData, float? baseValueOverride, int ticksGame)
    {
        float result = GetNewStatValue(requestData, baseValueOverride);
        return new CacheEnty(result, ticksGame + requestData.StatDef.cacheDuration);
    }

    protected abstract float GetNewStatValue(TRequestData requestData, float? baseValueOverride);
}