using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchTask : IExposable
{
    private BranchTaskDef def;
    public BranchTaskDef Def => def;
    public string Label => def.label;
    public BranchTaskType TaskType => def.taskType;

    private bool isOngoing;
    public bool IsOngoing => isOngoing;

    private int startTick;
    private int durationTick;

    public int StartTick => startTick;
    public int DurationTick => durationTick;
    public int DurationLeft => startTick + durationTick - Find.TickManager.TicksGame;
    public float Progress => Mathf.Clamp01(1f - DurationLeft / durationTick);

    protected BranchTask() { }

    /// <summary>
    /// 常用于反射构造，注意子类同参数构造函数需要非公开
    /// </summary>
    protected BranchTask(BranchTaskDef def) => this.def = def;

    public void StartTask(Branch branch, int durationTickOverride = -1)
    {
        startTick = Find.TickManager.TicksGame;
        durationTick = durationTickOverride > 0 ? durationTickOverride : TaskDurationTick(branch);
        isOngoing = true;
        PostTaskStart(branch);
    }

    protected virtual void PostTaskStart(Branch branch) { }

    public void EndTask(Branch branch)
    {
        isOngoing = false;
        PostTaskEnd(branch);
    }

    protected virtual void PostTaskEnd(Branch branch) { }

    protected virtual int TaskDurationTick(Branch branch)
    {
        return (int)(def.durationDays * 60000f);
    }

    public virtual int BranchRestTick(Branch branch)
    {
        return (int)(def.restDays * 60000f);
    }

    public virtual void TickHour(Branch branch) { }

    public float TaskRisk(Branch branch)
    {
        if (def.hasRisk)
        {
            return CalculateTaskRisk(branch);
        }
        return 0f;
    }

    protected virtual float CalculateTaskRisk(Branch branch)
    {
        float riskProb = Def.baseRiskProbability;
        if (branch.PopulationHandler.PublicSecurity < 1f)
        {
            riskProb += (1f - branch.PopulationHandler.PublicSecurity);
        }
        riskProb *= branch.TaskHandler.CurRadicalismDegree switch
        {
            BranchTaskHandler.RadicalismDegree.StabilityFocused => 0.5f,
            BranchTaskHandler.RadicalismDegree.Standard => 1f,
            BranchTaskHandler.RadicalismDegree.Aggressive => 2f,
            _ => 1f
        };
        return riskProb;
    }

    public static BranchTask GenerateTask(BranchTaskDef def)
    {
        return (BranchTask)Activator.CreateInstance(
            type: def.taskClass,
            bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance,
            binder: null,
            args: [def],
            culture: null);
    }

    public virtual void ExposeData()
    {
        Scribe_Defs.Look(ref def, nameof(def));
        Scribe_Values.Look(ref isOngoing, nameof(isOngoing), defaultValue: false);
        Scribe_Values.Look(ref startTick, nameof(startTick), 0);
        Scribe_Values.Look(ref durationTick, nameof(durationTick), 0);
    }
}