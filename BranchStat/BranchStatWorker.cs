using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatWorker(BranchStatDef statDef)
{
    public readonly BranchStatDef StatDef = statDef ?? throw new ArgumentNullException(nameof(statDef));

    protected Dictionary<Branch, CacheEnty> temporaryStatCache = statDef.cacheable ? [] : null;

    public virtual float GetValue(Branch branch, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        if (!StatDef.cacheable)
        {
            return BranchStatUtility.GetNewStatValue(branch, StatDef, baseValueOverride);
        }

        int ticksGame = Find.TickManager.TicksGame;
        if (temporaryStatCache.TryGetValue(branch, out CacheEnty cacheEnty))
        {
            if (immediateUpdate || cacheEnty.ExpiredTick < ticksGame)
            {
                CacheEnty newEnty = RecacheCacheEnty(StatDef, branch, baseValueOverride, ticksGame);
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
            CacheEnty newEnty = RecacheCacheEnty(StatDef, branch, baseValueOverride, ticksGame);
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

    public virtual void DeleteStatCache()
    {
        temporaryStatCache = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static CacheEnty RecacheCacheEnty(BranchStatDef statDef, Branch branch, float? baseValueOverride, int ticksGame)
    {
        float result = BranchStatUtility.GetNewStatValue(branch, statDef, baseValueOverride);
        return new CacheEnty(result, ticksGame + statDef.cacheDuration);
    }


    public struct CacheEnty : IEquatable<CacheEnty>
    {
        public float CacheValue; //缓存值
        public int ExpiredTick; //缓存时间戳

        public CacheEnty(float cacheValue, int expiredTick)
        {
            CacheValue = cacheValue;
            ExpiredTick = expiredTick;
        }
        public override readonly string ToString()
        {
            return $"BranchStatCacheEnty({CacheValue}, {ExpiredTick})";
        }

        public override int GetHashCode()
        {
            return CacheValue.GetHashCode() ^ ExpiredTick.GetHashCode();
        }
        public override readonly bool Equals(object obj)
        {
            return obj is CacheEnty other && Equals(other);
        }
        public readonly bool Equals(CacheEnty other)
        {
            return CacheValue == other.CacheValue && ExpiredTick == other.ExpiredTick;
        }
        public static bool operator ==(CacheEnty left, CacheEnty right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(CacheEnty left, CacheEnty right)
        {
            return !left.Equals(right);
        }
    }
}