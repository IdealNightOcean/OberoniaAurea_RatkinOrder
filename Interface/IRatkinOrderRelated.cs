using Verse;

namespace OberoniaAurea.RatkinOrder;

public interface IOnRatkinOrderRemoved
{
    void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder);
}

public interface ISingleRatkinOrderRelated : IOnRatkinOrderRemoved
{
    RatkinOrder RatkinOrder { get; }
    void InitRatkinOrder(RatkinOrder ratkinOrder);
}

public interface ISingleRatkinOrderRelatedReferenceable : ISingleRatkinOrderRelated, ILoadReferenceable;