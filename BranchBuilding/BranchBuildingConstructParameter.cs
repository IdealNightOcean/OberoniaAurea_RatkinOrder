using RimWorld.Planet;

namespace OberoniaAurea.RatkinOrder;

public struct BranchBuildingConstructParameter
{
    public Branch Branch;
    public BranchBuildingDef BuildingDef;
    private bool inSpecialSlot;
    public bool InSpecialSlot
    {
        get { return BuildingDef.isSpecial || inSpecialSlot; }
        set { inSpecialSlot = value; }
    }
    public bool ByPlayer;
    public Caravan Caravan;

    public readonly bool NeedDoubleConfirm => ByPlayer && BuildingDef.ConstructChecker.DoubleComfirm;
    public readonly void DoubleComfirm() => BuildingDef.ConstructChecker.DoubleComfirmAction(this);

    public BranchBuildingConstructParameter() { }

    public BranchBuildingConstructParameter(Branch branch, BranchBuildingDef buildingDef, bool inSpecialSlot)
    {
        Branch = branch;
        BuildingDef = buildingDef;
        this.inSpecialSlot = inSpecialSlot;
    }
}
