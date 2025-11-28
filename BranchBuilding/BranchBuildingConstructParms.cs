using RimWorld.Planet;

namespace OberoniaAurea.RatkinOrder;

public struct BranchBuildingConstructParms
{
    public Branch Branch;
    public BranchBuildingDef BuildingDef;

    public bool ByPlayer;
    public Caravan Caravan;

    public readonly bool NeedDoubleConfirm => ByPlayer && BuildingDef.ConstructChecker.DoubleComfirm;
    public readonly void DoubleComfirm() => BuildingDef.ConstructChecker.DoubleComfirmAction(this);

    public BranchBuildingConstructParms() { }

    public BranchBuildingConstructParms(Branch branch, BranchBuildingDef buildingDef)
    {
        Branch = branch;
        BuildingDef = buildingDef;
    }
}
