using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingConstructionRecord : IExposable
{
    private BranchBuildingDef buildingDef;
    private bool inSpecialSlot;
    private int durationTicks;
    public int DurationTicksLeft = -1;
    public BranchBuildingDef BuildingDef => buildingDef;
    public bool InSpecialSlot => inSpecialSlot;
    public bool HasFinished => DurationTicksLeft < 0;
    public float Progress => durationTicks > 0 ? Mathf.Clamp01(DurationTicksLeft / durationTicks) : 1f;

    public BranchBuildingConstructionRecord() { }
    public BranchBuildingConstructionRecord(BranchBuildingDef def, bool inSpecialSlot, int durationTicks)
    {
        buildingDef = def;
        this.inSpecialSlot = inSpecialSlot;
        DurationTicksLeft = durationTicks;
    }
    public void ExposeData()
    {
        Scribe_Defs.Look(ref buildingDef, "buildingDef");
        Scribe_Values.Look(ref inSpecialSlot, "inSpecialSlot", defaultValue: false);
        Scribe_Values.Look(ref durationTicks, "durationTicks", -1);
        Scribe_Values.Look(ref DurationTicksLeft, "DurationTicksLeft", -1);
    }
}

public class BranchFacilityConstructionRecord : IExposable
{
    private BranchFacilityDef facilityDef;
    private int durationTicks;
    public int DurationTicksLeft = -1;
    public BranchFacilityDef FacilityDef => facilityDef;
    public bool HasFinished => DurationTicksLeft < 0;
    public float Progress => durationTicks > 0 ? Mathf.Clamp01(DurationTicksLeft / durationTicks) : 1f;

    public BranchFacilityConstructionRecord() { }
    public BranchFacilityConstructionRecord(BranchFacilityDef def, int durationTicks)
    {
        facilityDef = def;
        DurationTicksLeft = durationTicks;
    }
    public void ExposeData()
    {
        Scribe_Defs.Look(ref facilityDef, "facilityDef");
        Scribe_Values.Look(ref durationTicks, "durationTicks", -1);
        Scribe_Values.Look(ref DurationTicksLeft, "DurationTicksLeft", -1);
    }
}