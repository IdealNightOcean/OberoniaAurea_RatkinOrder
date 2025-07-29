using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingConstructChecker
{
    public virtual bool DoubleComfirm => false;
    public virtual void DoubleComfirmAction(BranchBuildingConstructParameter constructParam) { }
    public virtual AcceptanceReport CanConstruct(BranchBuildingConstructParameter constructParam, bool resultOnly = false) { return true; }
}