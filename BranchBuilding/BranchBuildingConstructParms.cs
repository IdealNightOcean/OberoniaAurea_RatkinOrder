using Verse;

namespace OberoniaAurea.RatkinOrder;

public struct BranchBuildingConstructParms
{
    public Branch Branch { get; }
    public BranchBuildingDef BuildingDef { get; }

    public bool ByPlayer { get; set; }
    public Map Map { get; set; }

    public readonly bool NeedDoubleConfirm => ByPlayer && BuildingDef.ConstructChecker.DoubleComfirm;
    public readonly void DoubleComfirm() => BuildingDef.ConstructChecker?.DoubleComfirmAction(this);

    public BranchBuildingConstructParms(Branch branch, BranchBuildingDef buildingDef)
    {
        Branch = branch;
        BuildingDef = buildingDef;
    }
}
