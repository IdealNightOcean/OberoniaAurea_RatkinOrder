using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingConstructParameter : IExposable
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
    public Caravan caravan;

    public bool NeedDoubleConfirm => ByPlayer && BuildingDef.ConstructChecker.DoubleComfirm;
    public void DoubleComfirm() => BuildingDef.ConstructChecker.DoubleComfirmAction(this);

    public void ExposeData()
    {
        Scribe_References.Look(ref Branch, "Branch");
        Scribe_Defs.Look(ref BuildingDef, "BuildingDef");
        Scribe_Values.Look(ref inSpecialSlot, "inSpecialSlot", defaultValue: false);

        Scribe_Values.Look(ref ByPlayer, "ByPlayer", defaultValue: false);
        Scribe_References.Look(ref caravan, "caravan");
    }

    public BranchBuildingConstructParameter() { }

    public BranchBuildingConstructParameter(Branch branch, BranchBuildingDef buildingDef, bool inSpecialSlot)
    {
        Branch = branch;
        BuildingDef = buildingDef;
        this.inSpecialSlot = inSpecialSlot;
    }
}
