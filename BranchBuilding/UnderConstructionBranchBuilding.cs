using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class UnderConstructionBranchBuilding : IExposable
{
    private BranchBuildingDef buildingDef;
    private int durationTicks = -1;
    public int CompletedTick = -1;
    public BranchBuildingDef BuildingDef => buildingDef;
    public int DurationTicksLeft => CompletedTick - Find.TickManager.TicksGame;
    public float Progress => durationTicks > 0 ? Mathf.Clamp01(1f - DurationTicksLeft / (float)durationTicks) : 0f;

    public UnderConstructionBranchBuilding() { }
    public UnderConstructionBranchBuilding(BranchBuildingDef def, int durationTicks)
    {
        buildingDef = def;
        this.durationTicks = durationTicks;
        CompletedTick = Find.TickManager.TicksGame + durationTicks;
    }
    public void ExposeData()
    {
        Scribe_Defs.Look(ref buildingDef, "buildingDef");
        Scribe_Values.Look(ref durationTicks, "durationTicks", -1);
        Scribe_Values.Look(ref CompletedTick, "CompletedTick", -1);
    }
}