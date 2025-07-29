using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public struct BranchStatTransCacheEnty : IEquatable<BranchStatTransCacheEnty>
{
    public BranchStatTransformer cacheTrans; //缓存值
    public int expiredTick; //缓存时间戳

    public BranchStatTransCacheEnty(BranchStatTransformer cacheTrans, int expiredTick)
    {
        this.cacheTrans = cacheTrans;
        this.expiredTick = expiredTick;
    }
    public override readonly string ToString()
    {
        return $"BranchStatTransCacheEnty({expiredTick})";
    }

    public override int GetHashCode()
    {
        return cacheTrans.GetHashCode() ^ expiredTick.GetHashCode();
    }
    public override readonly bool Equals(object obj)
    {
        return obj is BranchStatCacheEnty other && Equals(other);
    }
    public readonly bool Equals(BranchStatTransCacheEnty other)
    {
        return cacheTrans == other.cacheTrans && expiredTick == other.expiredTick;
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

public class BranchStatWorker_Transformer(BranchStatDef stat) : BranchStatWorker(stat)
{
    private Dictionary<Branch, BranchStatTransCacheEnty> temporaryStatCache;

    public override void InitCacheability()
    {
        if (Stat.cacheable)
        {
            temporaryStatCache = [];
        }
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
            if (immediateUpdate || cacheEnty.expiredTick < ticksGame)
            {
                BranchStatTransCacheEnty newEnty = RecacheCacheEnty(Stat, branch, ticksGame);
                temporaryStatCache[branch] = newEnty;
                return BranchStatUtility.GetNewStatValueFormTrans(branch, Stat, newEnty.cacheTrans, baseValueOverride);
            }
            else
            {
                return BranchStatUtility.GetNewStatValueFormTrans(branch, Stat, cacheEnty.cacheTrans, baseValueOverride);
            }
        }
        else
        {
            BranchStatTransCacheEnty newEnty = RecacheCacheEnty(Stat, branch, ticksGame);
            temporaryStatCache.Add(branch, newEnty);
            return BranchStatUtility.GetNewStatValueFormTrans(branch, Stat, newEnty.cacheTrans, baseValueOverride);
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
