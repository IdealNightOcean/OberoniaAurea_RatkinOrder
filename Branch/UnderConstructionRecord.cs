using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class UnderConstructionRecord<T> : IExposable where T : BranchConstructionDef, new()
{
    private T targetDef;
    private int durationTicks = -1;
    public int CompletedTick = -1;
    public T TargetDef => targetDef;
    public int DurationTicksLeft => Mathf.Max(0, CompletedTick - Find.TickManager.TicksGame);
    public float Progress => durationTicks > 0 ? 1f - DurationTicksLeft / (float)durationTicks : 0f;

    public UnderConstructionRecord() { }
    public UnderConstructionRecord(T targetDef, int durationTicks)
    {
        this.targetDef = targetDef;
        this.durationTicks = durationTicks;
        CompletedTick = Find.TickManager.TicksGame + durationTicks;
    }

    public virtual void ExposeData()
    {
        Scribe_Defs.Look(ref targetDef, "targetDef");
        Scribe_Values.Look(ref durationTicks, "durationTicks", -1);
        Scribe_Values.Look(ref CompletedTick, "CompletedTick", -1);
    }
}