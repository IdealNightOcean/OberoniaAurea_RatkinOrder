using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingConstructChecker
{
    public virtual bool DoubleComfirm => false;
    public virtual void DoubleComfirmAction(BranchBuildingConstructParms constructParam) { }
    public virtual AcceptanceReport CanConstruct(BranchBuildingConstructParms constructParam, bool resultOnly = false) { return true; }
}