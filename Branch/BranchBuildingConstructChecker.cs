using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingConstructChecker
{
    public virtual bool DoubleComfirm => false;
    public virtual void DoubleComfirmAction(Branch branch, BranchBuildingDef def, bool inSpecialSlot, Caravan caravan) { }
    public virtual AcceptanceReport CanConstruct(Branch branch, BranchBuildingDef def, bool inSpecialSlot, bool byPlayer, Caravan caravan = null, bool resultOnly = false) { return true; }
}