using Verse;

namespace OberoniaAurea.RatkinOrder;

public interface IOnBranchDestoryed : IOnRatkinOrderRemoved
{
    void Notify_BranchDestoryed(Branch branch);
}

public interface ISingleBranchRelated : IOnBranchDestoryed
{
    Branch Branch { get; }
    void InitOrderBranch(Branch branch);
}

public interface ISingleBranchRelatedReferenceable : ISingleBranchRelated, ILoadReferenceable;