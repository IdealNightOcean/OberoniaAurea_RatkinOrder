namespace OberoniaAurea.RatkinOrder;

public interface IRatkinOrderRelated
{
    void Notify_RatkinOrderRemoved(RatkinOrder order);
}

public interface IBranchRelated : IRatkinOrderRelated
{
    void Notify_BranchDestoryed(Branch branch);
}
