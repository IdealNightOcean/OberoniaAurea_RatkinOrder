using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchTaskHandler : IExposable, ITickHourOfDay, ITickDay
{
    private float BaseAutoStartTaskChance => branch.IsBranchOfType(Branch.BranchType.Mobile) ? 0.05f : 0.02f;

    [Unsaved] private readonly Branch branch;

    private BranchTask curTask;
    private int curTaskStartTick = -1;
    private int curTaskTickLeft = -1;
    private int squadRestEndTick = -1;

    private string stateStr;

    public BranchTask CurTask => curTask;
    public bool HasTask => curTask is not null;
    public int CurTaskDuration => curTaskStartTick > 0 ? Find.TickManager.TicksGame - curTaskStartTick : 0;
    public bool IsRestNow => Find.TickManager.TicksGame < squadRestEndTick;
    private bool IsCurTaskOngoing => curTaskTickLeft > 0;

    public string TaskState => curTask?.Def.LabelCap;

    private BranchTaskDef autoTargetTask;
    private int autoStartFailCount;
    private float autoStartTaskChance;
    public float AutoStartTaskChance => autoStartTaskChance;

    public BranchTaskHandler(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
        autoStartTaskChance = BaseAutoStartTaskChance;
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label("CurTask:");
        if (HasTask)
        {
            listing_Rect.SubLabel(TaskState, 0.8f);
            listing_Rect.SubLabel($"CurTaskStartTick: {curTaskStartTick}", 0.8f);
            listing_Rect.SubLabel($"CurTaskTickLeft: {curTaskTickLeft}", 0.8f);
        }
        else
        {
            listing_Rect.SubLabel("None", 0.8f);
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label($"IsRestNow: {IsRestNow}");
        listing_Rect.Label($"SquadRestEndTick: {squadRestEndTick}");

        listing_Rect.Gap(6f);
        listing_Rect.Label("AutoTargetTask:");
        if (autoTargetTask is not null)
        {
            listing_Rect.SubLabel($"{autoTargetTask.label}", 0.8f);
            listing_Rect.SubLabel($"AutoStartFailCount: {autoStartFailCount}", 0.8f);
            listing_Rect.SubLabel($"AutoStartTaskChance: {autoStartTaskChance}", 0.8f);
        }
        else
        {
            listing_Rect.SubLabel("None", 0.8f);
            listing_Rect.SubLabel($"AutoStartTaskChance: {autoStartTaskChance}", 0.8f);
        }
    }

    public void TickHour(int hourOfDay)
    {
        if (!branch.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.SquadStateDesc))
        {
            branch.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.SquadStateDesc, cdTicks: 9 * 2500);
            UpdateStateDesc(hourOfDay);
        }
        if (HasTask)
        {
            curTask.TickHour(branch);
            curTaskTickLeft -= 2500;
            if (curTaskTickLeft <= 0)
            {
                FinishCurTask();
            }
        }
    }

    public void TickDay()
    {
        if (!IsRestNow && !HasTask)
        {
            TryAutoStartNewTask();
        }
    }

    public AcceptanceReport CanSwitchToTask(BranchTaskDef newTaskDef, bool ignorePriority = false, bool resultOnly = false)
    {
        if (branch.IsOnJointPatrol())
        {
            return resultOnly ? false : "OARO_BranchOnJointPatrolNow".Translate();
        }

        if (!HasTask)
        {
            if (!newTaskDef.ignoreRest && IsRestNow)
            {
                return resultOnly ? false : "OARO_SquadIsRestNow".Translate();
            }

            return newTaskDef?.StartChecker.CanStartNow(branch) ?? true;
        }

        if (newTaskDef is null)
        {
            if (IsCurTaskOngoing && !curTask.Def.canInterrupted)
            {
                return resultOnly ? false : "OARO_TaskCannotBeInterrupted".Translate();
            }

            return true;
        }

        BranchTaskDef curTaskDef = curTask.Def;

        if (newTaskDef == curTaskDef)
        {
            return resultOnly ? false : "OARO_AlreadyDoingSameTask".Translate();
        }

        if (IsCurTaskOngoing && !curTaskDef.canInterrupted)
        {
            return resultOnly ? false : "OARO_TaskCannotBeInterrupted".Translate();
        }

        if (newTaskDef == curTaskDef.nextTask)
        {
            return newTaskDef.StartChecker.CanStartNow(branch, resultOnly);
        }

        if (!ignorePriority && curTaskDef.priority >= newTaskDef.priority)
        {
            return resultOnly ? false : "OARO_TaskPriorityHigherOrEqual".Translate();
        }

        return newTaskDef.StartChecker.CanStartNow(branch, resultOnly);
    }

    public void FinishCurTask()
    {
        if (!HasTask)
        {
            return;
        }

        curTaskTickLeft = 0;
        if (curTask.Def.nextTask is null)
        {
            EndCurTask(startRest: true);
        }
        else
        {
            TrySwitchToTask(curTask.Def.nextTask);
        }
    }

    public bool TrySwitchToTask(BranchTaskDef newTaskDef)
    {
        if (CanSwitchToTask(newTaskDef, resultOnly: true))
        {
            EndCurTask(startRest: false);
            return StartTask(newTaskDef);
        }
        else if (!IsCurTaskOngoing)
        {
            EndCurTask(startRest: true);
        }

        return false;
    }

    private bool StartTask(BranchTaskDef newTaskDef)
    {
        try
        {
            curTask = BranchTask.MakeTask(newTaskDef);
            curTaskStartTick = Find.TickManager.TicksGame;
            curTaskTickLeft = curTask.TaskDurationTick(branch);
            curTask.TaskStart(branch);
        }
        catch (Exception ex)
        {
            Log.Error($"Fail to switch to task {newTaskDef}: {ex}");
            ClearCurTask();
            return false;
        }

        branch.EffectTags.IncrementTagsValue(newTaskDef.effectFlags, addIfMiss: true);
        autoStartTaskChance = BaseAutoStartTaskChance;
        if (newTaskDef == autoTargetTask)
        {
            ClearAutoTargetTask();
        }
        return true;
    }

    public void EndCurTask(bool startRest)
    {
        bool interrupt = IsCurTaskOngoing;
        if (HasTask)
        {
            branch.EffectTags.DecrementTagsValue(curTask.Def.effectFlags);
            curTask.TaskEnd(branch, interrupt);
            if (startRest)
            {
                squadRestEndTick = Find.TickManager.TicksGame + curTask.BranchRestTick(branch, interrupt);
            }
        }

        ClearCurTask();
    }

    private void ClearCurTask()
    {
        curTask = null;
        curTaskStartTick = -1;
        curTaskTickLeft = -1;
    }

    private void ClearAutoTargetTask()
    {
        autoTargetTask = null;
        autoStartFailCount = 0;
        autoStartTaskChance = BaseAutoStartTaskChance;
    }

    private string GetTaskAutoStartDesc()
    {
        if (HasTask)
        {
            return "OARO_OngoingTask".Translate().Colorize(Color.gray);
        }
        if (IsRestNow)
        {
            return "OARO_SquadRestNow".Translate(autoStartTaskChance.ToStringPercent()).Colorize(Color.green);
        }
        if (autoTargetTask is null)
        {
            return "OARO_SquadRestNow".Translate(autoStartTaskChance.ToStringPercent()).Colorize(Color.green);
        }

        if (autoTargetTask.StartChecker.CanStartNow(branch, resultOnly: true))
        {
            return "OARO_FullyTargetTaskDefPerp".Translate(autoStartTaskChance.ToStringPercent()).Colorize(Color.yellow);
        }
        else
        {
            return "OARO_InsufficientTargetTaskDefPerp".Translate(autoStartTaskChance.ToStringPercent()).Colorize(Color.red);
        }
    }

    private void TryAutoStartNewTask()
    {
        autoStartTaskChance += branch.IsBranchOfType(Branch.BranchType.Mobile) ? 0.01f : 0.005f;
        float usedChance = autoStartTaskChance;
        if (branch.Supply >= 1f && branch.Squad.MemberPercentage >= 1f)
        {
            usedChance += 0.5f;
        }

        if (autoStartFailCount >= 10 || autoTargetTask is null)
        {
            autoTargetTask = DefDatabase<BranchTaskDef>.AllDefs.Where(t => t.canBeRandomlyChosen).RandomElementByWeightWithFallback(WeightSelector, BranchTaskDefOf.OARO_JurisdictionDutyPerp);
        }

        if (Rand.Chance(usedChance) && autoTargetTask is not null)
        {
            if (TrySwitchToTask(autoTargetTask))
            {
                ClearAutoTargetTask();
            }
            else
            {
                autoStartFailCount++;
            }
        }

        float WeightSelector(BranchTaskDef def) => def.StartChecker?.RandomlyChosenWeight(branch) ?? 0f;
    }

    private void UpdateStateDesc(int hourOfDay)
    {
        if (TaskState is not null)
        {
            return;
        }
        if (hourOfDay <= 5 || hourOfDay >= 21)
        {
            stateStr = "OARO_SquadState_Rest".Translate();
            return;
        }

        stateStr = "OARO_SquadState_Idle".Translate();
    }

    public void ExposeData()
    {
        Scribe_Deep.Look(ref curTask, "curTask");
        Scribe_Values.Look(ref curTaskStartTick, "curTaskStartTick", -1);
        Scribe_Values.Look(ref curTaskTickLeft, "curTaskTickLeft", -1);
        Scribe_Values.Look(ref squadRestEndTick, "squadRestEndTick", -1);

        Scribe_Defs.Look(ref autoTargetTask, "autoTargetTask");
        Scribe_Values.Look(ref autoStartFailCount, "autoStartFailCount", 0);
        Scribe_Values.Look(ref autoStartTaskChance, "autoStartTaskChance", 0f);
    }

    internal void PostLoadInit()
    {
        if (curTask is not null)
        {
            branch.EffectTags.IncrementTagsValue(curTask.Def.effectFlags, addIfMiss: true);
        }
    }
}