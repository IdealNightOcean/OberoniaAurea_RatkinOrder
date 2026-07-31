using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame.DataLibrary;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 分部任务
/// </summary>
public class BranchTask : IExposable
{
    protected BranchTaskDef def;
    public BranchTaskDef Def => def;
    public string Label => def.label;
    public KnightChivalryDef TaskChivalry => def.chivalry;

    protected Branch branch;
    public Branch Branch => branch;

    protected bool isOngoing;
    public bool IsOngoing => isOngoing;

    protected int startTick;
    protected int durationTick;

    public int StartTick => startTick;
    public int DurationTick => durationTick;
    public int DurationLeft => startTick + durationTick - Find.TickManager.TicksGame;
    public float Progress => Mathf.Clamp01(1f - DurationLeft / durationTick);

    protected BranchTask() { }

    public static BranchTask GenerateTask(BranchTaskDef def, Branch branch)
    {
        BranchTask task = (BranchTask)Activator.CreateInstance(def.taskClass);
        task.def = def;
        task.branch = branch;
        return task;
    }

    public void StartTask(int durationTickOverride = -1)
    {
        startTick = Find.TickManager.TicksGame;
        durationTick = durationTickOverride > 0 ? durationTickOverride : TaskDurationTick();
        isOngoing = true;
        PostTaskStart();
    }

    public void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        int elapsed = Find.TickManager.TicksGame - startTick;

        if (progress < 0.001f)
        {
            durationTick = int.MaxValue;
            return;
        }

        durationTick = Mathf.RoundToInt(elapsed / progress);
    }

    public void SetDurationTickLeft(int durationTickLeft)
    {
        durationTickLeft = Mathf.Max(0, durationTickLeft);
        int elapsed = Find.TickManager.TicksGame - startTick;
        durationTick = elapsed + durationTickLeft;
    }

    protected virtual void PostTaskStart()
    {
        Map orderStationMap = OrderStationHandler.Instance?.OrderStationMap;
        if (!branch.IsBranchOfType(Branch.BranchType.Friendly) && (orderStationMap is null || !branch.IsInAffectedRange(orderStationMap.Tile)))
        {
            return;
        }

        Messages.Message(
            text: "OARO_Message_BranchTaskStarted".Translate(branch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName), Def.Named(KeyLibrary_FormatArgName.DEF)),
            def: MessageTypeDefOf.NeutralEvent);
    }

    public void EndTask(bool interrupt)
    {
        isOngoing = false;
        PostTaskEnd(interrupt);
    }

    protected virtual void PostTaskEnd(bool interrupt)
    {
        Map orderStationMap = OrderStationHandler.Instance?.OrderStationMap;
        if (!branch.IsBranchOfType(Branch.BranchType.Friendly) && (orderStationMap is null || !branch.IsInAffectedRange(orderStationMap.Tile)))
        {
            return;
        }

        Messages.Message(
            text: "OARO_Message_BranchTaskEnded".Translate(branch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName), Def.Named(KeyLibrary_FormatArgName.DEF)),
            def: MessageTypeDefOf.NeutralEvent);
    }

    protected virtual int TaskDurationTick() => (int)(def.durationDays * 60000f);

    public virtual void TickHour() { }

    public virtual string ExpectedRevenue() => string.Empty;

    public float TaskRisk()
    {
        if (def.hasRisk)
        {
            return CalculateTaskRisk();
        }
        return 0f;
    }

    protected virtual float CalculateTaskRisk()
    {
        float riskProb = Def.baseRiskProbability;
        if (branch.PopulationHandler.PublicSecurity < 1f && !branch.EffectTags.HasTag(KeyLibrary_EffectTag.DangerWarning))
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
        return Mathf.Clamp01(riskProb);
    }

    public virtual void ExposeData()
    {
        Scribe_Defs.Look(ref def, nameof(def));
        Scribe_References.Look(ref branch, nameof(branch));

        Scribe_Values.Look(ref isOngoing, nameof(isOngoing), defaultValue: false);
        Scribe_Values.Look(ref startTick, nameof(startTick), 0);
        Scribe_Values.Look(ref durationTick, nameof(durationTick), 0);
    }
}