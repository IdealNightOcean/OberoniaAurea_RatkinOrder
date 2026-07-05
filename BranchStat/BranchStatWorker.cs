using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatWorker(BranchStatDef statDef)
{
    public readonly BranchStatDef StatDef = statDef ?? throw new ArgumentNullException(nameof(statDef));

    private readonly Dictionary<Branch, CacheEnty> temporaryStatCache = statDef.cacheable ? new(8) : null;

    public float GetValue(Branch branch, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        if (!StatDef.cacheable)
            return BranchStatUtility.GetNewStatValue(branch, StatDef, baseValueOverride);

        int ticksGame = Find.TickManager.TicksGame;
        if (temporaryStatCache.TryGetValue(branch, out CacheEnty cacheEnty))
        {
            if (immediateUpdate || cacheEnty.ExpiredTick < ticksGame)
            {
                CacheEnty newEnty = BuildNewCacheEnty(StatDef, branch, baseValueOverride, ticksGame);
                temporaryStatCache[branch] = newEnty;
                return newEnty.CacheValue;
            }
            else
            {
                return cacheEnty.CacheValue;
            }
        }
        else
        {
            CacheEnty newEnty = BuildNewCacheEnty(StatDef, branch, baseValueOverride, ticksGame);
            temporaryStatCache.Add(branch, newEnty);
            return newEnty.CacheValue;
        }
    }

    public void UpdateStatCache(Branch branch, float value)
    {
        if (StatDef.cacheable)
        {
            temporaryStatCache[branch] = new CacheEnty(value, Find.TickManager.TicksGame + StatDef.cacheDuration);
        }
    }

    public void DeleteStatCache() => temporaryStatCache.Clear();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static CacheEnty BuildNewCacheEnty(BranchStatDef statDef, Branch branch, float? baseValueOverride, int ticksGame)
    {
        float result = BranchStatUtility.GetNewStatValue(branch, statDef, baseValueOverride);
        return new CacheEnty(result, ticksGame + statDef.cacheDuration);
    }
}