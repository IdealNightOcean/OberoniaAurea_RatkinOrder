using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public struct BranchStatTransCacheEnty : IEquatable<BranchStatTransCacheEnty>
{
    public BranchStatTransformer CacheTrans; //缓存值
    public int ExpiredTick; //缓存时间戳

    public BranchStatTransCacheEnty(BranchStatTransformer cacheTrans, int expiredTick)
    {
        CacheTrans = cacheTrans;
        ExpiredTick = expiredTick;
    }
    public override readonly string ToString()
    {
        return $"BranchStatTransCacheEnty({ExpiredTick})";
    }

    public override int GetHashCode()
    {
        return CacheTrans.GetHashCode() ^ ExpiredTick.GetHashCode();
    }
    public override readonly bool Equals(object obj)
    {
        return obj is BranchStatCacheEnty other && Equals(other);
    }
    public readonly bool Equals(BranchStatTransCacheEnty other)
    {
        return CacheTrans == other.CacheTrans && ExpiredTick == other.ExpiredTick;
    }
    public static bool operator ==(BranchStatTransCacheEnty left, BranchStatTransCacheEnty right)
    {
        return left.Equals(right);
    }
    public static bool operator !=(BranchStatTransCacheEnty left, BranchStatTransCacheEnty right)
    {
        return !left.Equals(right);
    }
}

public class BranchStatWorker_Transformer : BranchStatWorker
{
    private new Dictionary<Branch, BranchStatTransCacheEnty> temporaryStatCache;

    public BranchStatWorker_Transformer(BranchStatDef stat) : base(stat)
    {
        temporaryStatCache = stat.cacheable ? [] : null;
        base.temporaryStatCache = null;
    }

    public override float GetValue(Branch branch, float? baseValueOverride = null, bool immediateUpdate = false)
    {
        if (!Stat.cacheable)
        {
            return BranchStatUtility.GetNewStatValue(branch, Stat, baseValueOverride);
        }

        int ticksGame = Find.TickManager.TicksGame;
        if (temporaryStatCache.TryGetValue(branch, out BranchStatTransCacheEnty cacheEnty))
        {
            if (immediateUpdate || cacheEnty.ExpiredTick < ticksGame)
            {
                BranchStatTransCacheEnty newEnty = RecacheCacheEnty(Stat, branch, ticksGame);
                temporaryStatCache[branch] = newEnty;
                return BranchStatUtility.GetNewStatValueFormTrans(branch, Stat, newEnty.CacheTrans, baseValueOverride);
            }
            else
            {
                return BranchStatUtility.GetNewStatValueFormTrans(branch, Stat, cacheEnty.CacheTrans, baseValueOverride);
            }
        }
        else
        {
            BranchStatTransCacheEnty newEnty = RecacheCacheEnty(Stat, branch, ticksGame);
            temporaryStatCache.Add(branch, newEnty);
            return BranchStatUtility.GetNewStatValueFormTrans(branch, Stat, newEnty.CacheTrans, baseValueOverride);
        }
    }

    public override void DeleteStatCache()
    {
        temporaryStatCache = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BranchStatTransCacheEnty RecacheCacheEnty(BranchStatDef statDef, Branch branch, int ticksGame)
    {
        BranchStatUtility.TryGetStatTransformer(branch, statDef, out BranchStatTransformer result);
        return new BranchStatTransCacheEnty(result, ticksGame + statDef.cacheDuration);
    }
}
