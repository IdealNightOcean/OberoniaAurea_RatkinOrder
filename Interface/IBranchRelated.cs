using Verse;

namespace OberoniaAurea.RatkinOrder;

public interface IOnBranchDestroyed : IOnRatkinOrderRemoved
{
    void Notify_BranchDestroyed(Branch branch);
}

public interface ISingleBranchRelated : IOnBranchDestroyed
{
    Branch Branch { get; }
    void SetOrderBranch(Branch branch);
}

public interface ISingleBranchRelatedReferenceable : ISingleBranchRelated, ILoadReferenceable;