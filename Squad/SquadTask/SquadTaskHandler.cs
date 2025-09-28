using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class SquadTaskHandler : IExposable, ITickHourOfDay, ITickDay
{
    private float BaseAutoStartTaskChance => Squad.IsBranchSquadOfType(BranchType.Mobile) ? 0.05f : 0.02f;

    [Unsaved] public readonly Squad Squad;

    private SquadTask curTask;
    private int curTaskStartTick = -1;
    private int curTaskTickLeft = -1;
    private int squadRestEndTick = -1;

    public SquadTask CurTask => curTask;
    public bool HasTask => curTask is not null;
    public int CurTaskDuration => curTaskStartTick > 0 ? Find.TickManager.TicksGame - curTaskStartTick : 0;
    public bool IsCurTaskOngoing => curTaskTickLeft > 0;
    public bool IsRestNow => Find.TickManager.TicksGame < squadRestEndTick;

    public string TaskState => curTask?.Def.LabelCap;
    public bool BlockSupport => curTask?.Def.blockSupport ?? false; //是否阻止恢复
    public bool BlockRecover => curTask?.Def.blockRecover ?? false; //是否阻止恢复
    public bool BlockBombard => curTask?.Def.blockBombard ?? false; //是否阻止恢复


    private SquadTaskDef autoTargetTask;
    private int autoStartFailCount;
    private float autoStartTaskChance;
    public float AutoStartTaskChance => autoStartTaskChance;

    public SquadTaskHandler(Squad squad)
    {
        Squad = squad ?? throw new ArgumentNullException(nameof(squad));
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

            listing_Rect.Gap(3f);
            listing_Rect.SubLabel($"BlockSupport: {BlockSupport}", 0.8f);
            listing_Rect.SubLabel($"BlockRecover: {BlockRecover}", 0.8f);
            listing_Rect.SubLabel($"BlockBombard: {BlockBombard}", 0.8f);
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
        if (HasTask)
        {
            curTask.TickHour(Squad);
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

    public AcceptanceReport CanSwitchToTask(SquadTaskDef newTaskDef, bool ignorePriority = false, bool resultOnly = false)
    {
        if (!HasTask)
        {
            if (!newTaskDef.ignoreRest && IsRestNow)
            {
                return resultOnly ? false : "OARO_SquadIsRestNow".Translate();
            }

            return newTaskDef?.StartChecker.CanStartNow(Squad) ?? true;
        }

        if (newTaskDef is null)
        {
            if (IsCurTaskOngoing && !curTask.Def.canInterrupt)
            {
                return resultOnly ? false : "OARO_TaskCannotBeInterrupted".Translate();
            }

            return true;
        }

        SquadTaskDef curTaskDef = curTask.Def;

        if (newTaskDef == curTaskDef)
        {
            return resultOnly ? false : "OARO_AlreadyDoingSameTask".Translate();
        }

        if (IsCurTaskOngoing && !curTaskDef.canInterrupt)
        {
            return resultOnly ? false : "OARO_TaskCannotBeInterrupted".Translate();
        }

        if (newTaskDef == curTaskDef.nextTaskStatus)
        {
            return newTaskDef.StartChecker.CanStartNow(Squad, resultOnly);
        }

        if (!ignorePriority && curTaskDef.priority >= newTaskDef.priority)
        {
            return resultOnly ? false : "OARO_TaskPriorityHigherOrEqual".Translate();
        }

        return newTaskDef.StartChecker.CanStartNow(Squad, resultOnly);
    }

    public void FinishCurTask()
    {
        if (!HasTask)
        {
            return;
        }

        curTaskTickLeft = 0;
        if (curTask.Def.nextTaskStatus is null)
        {
            EndCurrentTask(startRest: true);
        }
        else
        {
            TrySwitchToTask(curTask.Def.nextTaskStatus);
        }
    }

    public bool TrySwitchToTask(SquadTaskDef newTaskDef)
    {

        if (CanSwitchToTask(newTaskDef, resultOnly: true))
        {
            EndCurrentTask(startRest: false);
            return StartTask(newTaskDef);
        }
        else if (!IsCurTaskOngoing)
        {
            EndCurrentTask(startRest: true);
        }

        return false;
    }

    public bool StartTask(SquadTaskDef newTaskDef)
    {
        try
        {
            if (newTaskDef is null)
            {
                EndCurrentTask(startRest: true);
                return true;
            }

            curTask = SquadTask.MakeTask(newTaskDef);
            curTaskStartTick = Find.TickManager.TicksGame;
            curTaskTickLeft = curTask.TaskDurationTick(Squad);
            curTask.TaskStart(Squad);

            autoStartTaskChance = BaseAutoStartTaskChance;
            if (newTaskDef == autoTargetTask)
            {
                ClearAutoTargetTask();
            }
            return true;

        }
        catch (Exception ex)
        {
            Log.Error($"Fail to switch to task {newTaskDef}: {ex}");
            ClearCurTask();
            return false;
        }
    }

    public bool StartTask(SquadTask newTask)
    {
        try
        {
            if (newTask is null)
            {
                EndCurrentTask(startRest: true);
                return true;
            }

            curTask = newTask;
            curTaskStartTick = Find.TickManager.TicksGame;
            curTaskTickLeft = curTask.TaskDurationTick(Squad);
            curTask.TaskStart(Squad);

            autoStartTaskChance = BaseAutoStartTaskChance;
            if (newTask.Def == autoTargetTask)
            {
                ClearAutoTargetTask();
            }
            return true;

        }
        catch (Exception ex)
        {
            Log.Error($"Fail to switch to task {newTask.Def}: {ex}");
            EndCurrentTask(startRest: false);
            return false;
        }
    }

    public void EndCurrentTask(bool startRest)
    {
        bool interrupt = IsCurTaskOngoing;
        if (HasTask)
        {
            curTask.TaskEnd(Squad, interrupt);
            if (startRest)
            {
                squadRestEndTick = Find.TickManager.TicksGame + curTask.SquadRestTick(Squad, interrupt);
            }
        }

        ClearCurTask();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearCurTask()
    {
        curTask = null;
        curTaskStartTick = -1;
        curTaskTickLeft = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        if (autoTargetTask.StartChecker.CanStartNow(Squad, resultOnly: true))
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
        autoStartTaskChance += Squad.IsBranchSquadOfType(BranchType.Mobile) ? 0.01f : 0.005f;
        float usedChance = autoStartTaskChance;
        if (Squad.SquadStat.MemberPercentage >= 1f && Squad.SquadStat.Supply >= 1f)
        {
            usedChance += 0.5f;
        }

        if (autoStartFailCount >= 10 || autoTargetTask is null)
        {
            autoTargetTask = DefDatabase<SquadTaskDef>.AllDefs.Where(t => t.canBeRandomlyChosen).RandomElementByWeightWithFallback(WeightSelector, SquadTaskDefOf.OARO_Squad_JurisdictionDutyPerp);
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

        float WeightSelector(SquadTaskDef def) => def.StartChecker?.RandomlyChosenWeight(Squad) ?? 0f;
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
}