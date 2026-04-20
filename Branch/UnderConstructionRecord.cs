using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 建设中项目记录 - 包含目标定义、剩余建设时间、完成时刻等相关内容
/// </summary>
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
        Scribe_Defs.Look(ref targetDef, nameof(targetDef));
        Scribe_Values.Look(ref durationTicks, nameof(durationTicks), -1);
        Scribe_Values.Look(ref CompletedTick, nameof(CompletedTick), -1);
    }
}