using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchTaskHandler : IExposable, ITickHourOfDay, ITickDay
{

    [Unsaved] private readonly Branch branch;

    private BranchTask curTask;
    private int restEndTick = -1;

    public BranchTask CurTask => curTask;
    public bool HasTask => curTask is not null;
    public bool IsRestNow => Find.TickManager.TicksGame < restEndTick;

    public string TaskLabel => curTask?.Def.label;

    private BranchTaskDef autoTargetTask;
    private int autoStartFailCount;
    private float autoStartTaskChance;
    public float AutoStartTaskChance => autoStartTaskChance;

    internal BranchTaskHandler(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
        autoStartTaskChance = BaseAutoStartTaskChance(branch);
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        if (HasTask)
        {
            listing_Rect.Label($"CurTask: {TaskLabel}");

            listing_Rect.SubLabel($"StartTask: {curTask.StartTick}", 0.8f);
            listing_Rect.SubLabel($"DurationTick: {curTask.DurationTick}", 0.8f);
            listing_Rect.SubLabel($"DurationLeft: {curTask.DurationLeft}", 0.8f);
            listing_Rect.SubLabel($"Progress: {curTask.Progress}", 0.8f);
            listing_Rect.SubLabel($"IsOngoing: {curTask.IsOngoing}", 0.8f);
        }
        else
        {
            listing_Rect.Label("CurTask: None");
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label($"IsRestNow: {IsRestNow}");
        listing_Rect.Label($"RestEndTick: {restEndTick}");

        listing_Rect.Gap(6f);
        if (autoTargetTask is null)
        {
            listing_Rect.Label("AutoTargetTask: None");
            listing_Rect.SubLabel($"AutoStartTaskChance: {autoStartTaskChance}", 0.8f);
        }
        else
        {
            listing_Rect.Label($"AutoTargetTask: {autoTargetTask.label}");
            listing_Rect.SubLabel($"AutoStartFailCount: {autoStartFailCount}", 0.8f);
            listing_Rect.SubLabel($"AutoStartTaskChance: {autoStartTaskChance}", 0.8f);
        }
    }

    public void TickHour(int hourOfDay)
    {
        if (HasTask)
        {
            curTask.TickHour(branch);
            if (curTask.DurationLeft <= 0)
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
            if (curTask.IsOngoing && !curTask.Def.canInterrupted)
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

        if (curTask.IsOngoing && !curTaskDef.canInterrupted)
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

        if (curTask.Def.nextTask is null)
        {
            EndCurTask(startRest: true);
        }
        else
        {
            TrySwitchToTask(curTask.Def.nextTask, endCurIfCantSwitch: true);
        }
    }

    public bool TrySwitchToTask(BranchTaskDef newTaskDef, bool forced = false, bool endCurIfCantSwitch = false)
    {
        if (forced || CanSwitchToTask(newTaskDef, resultOnly: true))
        {
            if (newTaskDef is null)
            {
                EndCurTask(startRest: true);
                return true;
            }
            else
            {
                EndCurTask(startRest: false);
                return StartTask(newTaskDef);
            }
        }

        if (endCurIfCantSwitch)
        {
            EndCurTask(startRest: true);
        }

        return false;
    }

    private bool StartTask(BranchTaskDef newTaskDef)
    {
        try
        {
            curTask = BranchTask.GenerateTask(newTaskDef);
            curTask.StartTask(branch);
        }
        catch (Exception ex)
        {
            Log.Error($"Fail to switch to task {newTaskDef}: {ex}");
            curTask = null;
            return false;
        }

        branch.EffectTags.IncrementTagsValue(newTaskDef.effectFlags, addIfMiss: true);
        autoStartTaskChance = BaseAutoStartTaskChance(branch);
        if (newTaskDef == autoTargetTask)
        {
            ClearAutoTargetTask();
        }

        branch.WorkStateDirty = true;
        return true;
    }

    public void EndCurTask(bool startRest)
    {
        if (!HasTask)
        {
            return;
        }

        branch.EffectTags.DecrementTagsValue(curTask.Def.effectFlags);
        curTask.EndTask(branch);
        if (startRest)
        {
            restEndTick = Find.TickManager.TicksGame + curTask.BranchRestTick(branch);
        }

        curTask = null;
        branch.WorkStateDirty = true;
    }

    private void ClearAutoTargetTask()
    {
        autoTargetTask = null;
        autoStartFailCount = 0;
        autoStartTaskChance = BaseAutoStartTaskChance(branch);
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
            autoTargetTask = DefDatabase<BranchTaskDef>.AllDefs.Where(t => t.canBeRandomlyChosen).RandomElementByWeightWithFallback(WeightSelector, BranchTaskDefOf.OARO_JurisdictionDutyPrep);
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

    public void ExposeData()
    {
        Scribe_Deep.Look(ref curTask, "curTask");
        Scribe_Values.Look(ref restEndTick, "squadRestEndTick", -1);

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

    private static float BaseAutoStartTaskChance(Branch branch) => branch.IsBranchOfType(Branch.BranchType.Mobile) ? 0.05f : 0.02f;
}