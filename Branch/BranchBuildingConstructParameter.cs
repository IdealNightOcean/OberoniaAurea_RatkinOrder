using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingConstructParameter : IExposable
{
    public Branch branch;
    public BranchBuildingDef buildingDef;
    private bool inSpecialSlot;
    public bool InSpecialSlot
    {
        get { return buildingDef.isSpecial || inSpecialSlot; }
        set { inSpecialSlot = value; }
    }
    public bool byPlayer;
    public Caravan caravan;

    public bool NeedDoubleConfirm => byPlayer && buildingDef.ConstructChecker.DoubleComfirm;
    public void DoubleComfirm() => buildingDef.ConstructChecker.DoubleComfirmAction(this);

    public void ExposeData()
    {
        Scribe_References.Look(ref branch, "branch");
        Scribe_Defs.Look(ref buildingDef, "buildingDef");
        Scribe_Values.Look(ref inSpecialSlot, "inSpecialSlot", defaultValue: false);

        Scribe_Values.Look(ref inSpecialSlot, "inSpecialSlot", defaultValue: false);
        Scribe_References.Look(ref caravan, "caravan");
    }

    public BranchBuildingConstructParameter() { }

    public BranchBuildingConstructParameter(Branch branch, BranchBuildingDef buildingDef, bool inSpecialSlot)
    {
        this.branch = branch;
        this.buildingDef = buildingDef;
        this.inSpecialSlot = inSpecialSlot;
    }
}
