using System;

namespace OberoniaAurea.RatkinOrder;

public struct CacheEnty(float cacheValue, int expiredTick) : IEquatable<CacheEnty>
{
    public float CacheValue = cacheValue; //缓存值
    public int ExpiredTick = expiredTick; //缓存时间戳

    public override readonly string ToString() => $"[OARO] StatCacheEnty({CacheValue}, {ExpiredTick})";

    public override int GetHashCode() => CacheValue.GetHashCode() ^ ExpiredTick.GetHashCode();

    public override readonly bool Equals(object obj) => obj is CacheEnty other && Equals(other);

    public readonly bool Equals(CacheEnty other) => CacheValue == other.CacheValue && ExpiredTick == other.ExpiredTick;

    public static bool operator ==(CacheEnty left, CacheEnty right) => left.Equals(right);

    public static bool operator !=(CacheEnty left, CacheEnty right) => !left.Equals(right);
}
