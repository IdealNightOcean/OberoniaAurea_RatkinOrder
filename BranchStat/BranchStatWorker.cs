using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public struct BranchStatCacheEnty : IEquatable<BranchStatCacheEnty>
{
    public float CacheValue; //缓存值
    public int ExpiredTick; //缓存时间戳

    public BranchStatCacheEnty(float cacheValue, int expiredTick)
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
        return obj is BranchStatCacheEnty other && Equals(other);
    }
    public readonly bool Equals(BranchStatCacheEnty other)
    {
        return CacheValue == other.CacheValue && ExpiredTick == other.ExpiredTick;
    }
    public static bool operator ==(BranchStatCacheEnty left, BranchStatCacheEnty right)
    {
        return left.Equals(right);
    }
    public static bool operator !=(BranchStatCacheEnty left, BranchStatCacheEnty right)
    {
        return !left.Equals(right);
    }
}

public class BranchStatWorker(BranchStatDef stat)
{
    public readonly BranchStatDef Stat = stat ?? throw new ArgumentNullException(nameof(stat));

    private Dictionary<Branch, BranchStatCacheEnty> temporaryStatCache;
    public virtual void InitCacheability()
    {
        if (Stat.cacheable)
        {
            temporaryStatCache = [];
        }
    }

    public virtual float GetValue(Branch branch, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        if (!Stat.cacheable)
        {
            return BranchStatUtility.GetNewStatValue(branch, Stat, baseValueOverride);
        }

        int ticksGame = Find.TickManager.TicksGame;
        if (temporaryStatCache.TryGetValue(branch, out BranchStatCacheEnty cacheEnty))
        {
            if (immediateUpdate || cacheEnty.ExpiredTick < ticksGame)
            {
                BranchStatCacheEnty newEnty = RecacheCacheEnty(Stat, branch, baseValueOverride, ticksGame);
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
            BranchStatCacheEnty newEnty = RecacheCacheEnty(Stat, branch, baseValueOverride, ticksGame);
            temporaryStatCache.Add(branch, newEnty);
            return newEnty.CacheValue;
        }
    }

    public virtual void DeleteStatCache()
    {
        temporaryStatCache = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BranchStatCacheEnty RecacheCacheEnty(BranchStatDef statDef, Branch branch, float? baseValueOverride, int ticksGame)
    {
        float result = BranchStatUtility.GetNewStatValue(branch, statDef, baseValueOverride);
        return new BranchStatCacheEnty(result, ticksGame + statDef.cacheDuration);
    }
}