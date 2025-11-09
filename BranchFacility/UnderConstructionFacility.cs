using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class UnderConstructionFacility : IExposable
{
    private BranchFacilityDef facilityDef;
    private int durationTicks = -1;
    public int CompletedTick = -1;
    public BranchFacilityDef FacilityDef => facilityDef;
    public int DurationTicksLeft => CompletedTick - Find.TickManager.TicksGame;
    public float Progress => durationTicks > 0 ? Mathf.Clamp01(1f - DurationTicksLeft / (float)durationTicks) : 0f;

    public UnderConstructionFacility() { }
    public UnderConstructionFacility(BranchFacilityDef def, int durationTicks)
    {
        facilityDef = def;
        this.durationTicks = durationTicks;
        CompletedTick = Find.TickManager.TicksGame + durationTicks;
    }
    public void ExposeData()
    {
        Scribe_Defs.Look(ref facilityDef, "facilityDef");
        Scribe_Values.Look(ref durationTicks, "durationTicks", -1);
        Scribe_Values.Look(ref CompletedTick, "CompletedTick", -1);
    }
}