using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingConstructionRecord
{
    public BranchBuildingDef BuildingDef;
    public bool InSpecialSlot;
    private int durationTicks;
    public int durationTicksLeft = -1;
    public float Progress => durationTicks > 0 ? Mathf.Clamp01(durationTicksLeft / durationTicks) : 1f;

    public bool HasFinished => durationTicksLeft < 0;

    public BranchBuildingConstructionRecord() { }
    public BranchBuildingConstructionRecord(BranchBuildingDef def, bool inSpecialSlot, int durationTicks)
    {
        BuildingDef = def;
        InSpecialSlot = inSpecialSlot;
        durationTicksLeft = durationTicks;
    }
    public void ExposeData()
    {
        Scribe_Defs.Look(ref BuildingDef, "BuildingDef");
        Scribe_Values.Look(ref InSpecialSlot, "InSpecialSlot", defaultValue: false);
        Scribe_Values.Look(ref durationTicks, "durationTicks", -1);
        Scribe_Values.Look(ref durationTicksLeft, "durationTicksLeft", -1);
    }
}