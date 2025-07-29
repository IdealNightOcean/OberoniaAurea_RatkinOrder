using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public struct BranchStatCacheEnty : IEquatable<BranchStatCacheEnty>
{
    public float cacheValue; //缓存值
    public int expiredTick; //缓存时间戳

    public BranchStatCacheEnty(float cacheValue, int expiredTick)
    {
        this.cacheValue = cacheValue;
        this.expiredTick = expiredTick;
    }
    public override readonly string ToString()
    {
        return $"BranchStatCacheEnty({cacheValue}, {expiredTick})";
    }

    public override int GetHashCode()
    {
        return cacheValue.GetHashCode() ^ expiredTick.GetHashCode();
    }
    public override readonly bool Equals(object obj)
    {
        return obj is BranchStatCacheEnty other && Equals(other);
    }
    public readonly bool Equals(BranchStatCacheEnty other)
    {
        return cacheValue == other.cacheValue && expiredTick == other.expiredTick;
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
            if (immediateUpdate || cacheEnty.expiredTick < ticksGame)
            {
                BranchStatCacheEnty newEnty = RecacheCacheEnty(Stat, branch, baseValueOverride, ticksGame);
                temporaryStatCache[branch] = newEnty;
                return newEnty.cacheValue;
            }
            else
            {
                return cacheEnty.cacheValue;
            }
        }
        else
        {
            BranchStatCacheEnty newEnty = RecacheCacheEnty(Stat, branch, baseValueOverride, ticksGame);
            temporaryStatCache.Add(branch, newEnty);
            return newEnty.cacheValue;
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