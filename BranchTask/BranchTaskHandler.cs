using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 分部任务处理器 - 负责管理分部当前任务、当前的专注任务、自动开始新任务的尝试、任务切换的条件判断等相关内容
/// </summary>
public class BranchTaskHandler : IExposable, ITickHourOfDay, ITickDay
{
    public enum RadicalismDegree
    {
        /// <summary>
        /// 常规
        /// </summary>
        Standard,
        /// <summary>
        /// 维稳
        /// </summary>
        StabilityFocused,
        /// <summary>
        /// 激进
        /// </summary>
        Aggressive
    }

    [Unsaved] private readonly Branch branch;

    private KnightChivalryDef focusedTaskChivalry;
    /// <summary>
    /// 专注任务类型
    /// </summary>
    public KnightChivalryDef FocusedTaskChivalry
    {
        get { return focusedTaskChivalry; }
        set
        {
            if (value != focusedTaskChivalry)
            {
                focusedTaskChivalry = value;
                branch.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.FocusedTaskTypeChanged, cdTicks: 5 * 60000, removeWhenExpired: true);
            }
        }
    }

    private RadicalismDegree curRadicalismDegree;
    /// <summary>
    /// 分部的激进程度
    /// </summary>
    public RadicalismDegree CurRadicalismDegree
    {
        get { return curRadicalismDegree; }
        set
        {
            if (value != curRadicalismDegree)
            {
                curRadicalismDegree = value;
                branch.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.RadicalismDegreeChanged, cdTicks: 5 * 60000, removeWhenExpired: true);
                if (IsRestNow)
                {
                    ResetRestTick();
                }
            }
        }
    }

    private BranchTask curTask;
    public BranchTask CurTask => curTask;
    public bool HasTask => curTask is not null;

    private int restEndTick = -1;
    public bool IsRestNow => Find.TickManager.TicksGame < restEndTick;

    private BranchTaskDef autoTargetTask;
    private int autoStartFailCount;
    private float autoStartTaskChance;
    public float AutoStartTaskChance => autoStartTaskChance * curRadicalismDegree switch
    {
        RadicalismDegree.Standard => 1f,
        RadicalismDegree.StabilityFocused => 0.5f,
        RadicalismDegree.Aggressive => 2f,
        _ => 2f
    };

    internal BranchTaskHandler(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
        autoStartTaskChance = BaseAutoStartTaskChance(branch);
    }

    public void ResetRestTick()
    {
        restEndTick = Find.TickManager.TicksGame + BranchRestTickByDegree(curRadicalismDegree);
    }

    public static int BranchRestTickByDegree(RadicalismDegree degree)
    {
        return degree switch
        {
            RadicalismDegree.StabilityFocused => 45 * 60000,
            RadicalismDegree.Aggressive => 15 * 60000,
            _ => 30 * 60000
        };
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref curRadicalismDegree, nameof(curRadicalismDegree), RadicalismDegree.Standard);
        Scribe_Defs.Look(ref focusedTaskChivalry, nameof(focusedTaskChivalry));

        Scribe_Deep.Look(ref curTask, nameof(curTask));
        Scribe_Values.Look(ref restEndTick, nameof(restEndTick), -1);

        Scribe_Defs.Look(ref autoTargetTask, nameof(autoTargetTask));
        Scribe_Values.Look(ref autoStartFailCount, nameof(autoStartFailCount), 0);
        Scribe_Values.Look(ref autoStartTaskChance, nameof(autoStartTaskChance), 0f);
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"专注任务类型: {FocusedTaskChivalry}");
        if (HasTask)
        {
            listing_Rect.Label($"当前任务: {curTask.Label}");

            listing_Rect.SubLabel($"开始Tick: {curTask.StartTick}", 0.8f);
            listing_Rect.SubLabel($"总持续Tick: {curTask.DurationTick}", 0.8f);
            listing_Rect.SubLabel($"剩余Tick: {curTask.DurationLeft}", 0.8f);
            listing_Rect.SubLabel($"任务进度: {curTask.Progress}", 0.8f);
            listing_Rect.SubLabel($"进行中: {curTask.IsOngoing}", 0.8f);
        }
        else
        {
            listing_Rect.Label("当前任务: 无");
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label($"是否休息中: {IsRestNow}");
        listing_Rect.Label($"休息结束Tick: {restEndTick}");

        listing_Rect.Gap(6f);
        if (autoTargetTask is null)
        {
            listing_Rect.Label("主动执勤目标任务: 无");
            listing_Rect.SubLabel($"主动执勤概率: {autoStartTaskChance}", 0.8f);
        }
        else
        {
            listing_Rect.Label($"主动执勤目标任务: {autoTargetTask.label}");
            listing_Rect.SubLabel($"主动执勤尝试失败次数: {autoStartFailCount}", 0.8f);
            listing_Rect.SubLabel($"主动执勤概率: {autoStartTaskChance}", 0.8f);
        }
    }

    public void TickHour(int hourOfDay)
    {
        if (HasTask)
        {
            curTask.TickHour();
            if (curTask.DurationLeft <= 0)
            {
                EndCurTask(startRest: true);
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

    public AcceptanceReport CanStartTask(BranchTaskDef newTaskDef, bool ignorePriority = false, bool resultOnly = false)
    {
        if (newTaskDef is null)
            return false;

        if (branch.IsOnJointPatrol())
        {
            return resultOnly ? false : "OARO_BranchOnJointPatrolNow".Translate();
        }

        int beAttackedCooling = branch.CooldownManager.GetCooldownTicksLeft(KeyLibrary_CDRecord.BeAttackedOnTask);
        if (beAttackedCooling > 0)
        {
            return resultOnly ? false : "OARO_Cooling_BeAttackedOnTask".Translate().Colorize(ColorLibrary.RedReadable)
                                        + ", "
                                        + "WaitTime".Translate(beAttackedCooling.ToStringTicksToPeriod());
        }

        if (HasTask)
        {
            BranchTaskDef curTaskDef = curTask.Def;
            if (newTaskDef == curTaskDef)
            {
                return resultOnly ? false : "OARO_AlreadyDoingSameTask".Translate();
            }

            if (curTask.IsOngoing && !curTaskDef.canInterrupted)
            {
                return resultOnly ? false : "OARO_TaskCannotBeInterrupted".Translate();
            }

            if (!ignorePriority && curTaskDef.priority >= newTaskDef.priority)
            {
                return resultOnly ? false : "OARO_TaskPriorityHigherOrEqual".Translate();
            }
        }
        else
        {
            if (!newTaskDef.ignoreRest && IsRestNow)
            {
                return resultOnly ? false : "OARO_SquadIsRestNow".Translate();
            }
        }

        return newTaskDef.StartChecker.CanStartNow(branch, resultOnly);
    }

    public bool TryStartTask(BranchTaskDef newTaskDef, bool forced = false)
    {
        if (newTaskDef is null)
            return false;

        if (forced || CanStartTask(newTaskDef, resultOnly: true))
        {
            return StartTask(newTaskDef);
        }

        return false;
    }

    public void EndCurTask(bool startRest)
    {
        if (!HasTask)
            return;

        branch.EffectTags.DecrementTagsValue(curTask.Def.effectFlags);
        curTask.EndTask();
        if (startRest)
        {
            restEndTick = Find.TickManager.TicksGame + curTask.BranchRestTick();
        }

        curTask = null;
        branch.MarkWorkStateDirty();
    }

    private bool StartTask(BranchTaskDef newTaskDef)
    {
        try
        {
            if (curTask is not null)
            {
                EndCurTask(startRest: false);
            }
            curTask = BranchTask.GenerateTask(newTaskDef, branch);
            curTask.StartTask();
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"为分部 {branch} 开始任务 {newTaskDef} ",
                typeName: nameof(BranchTaskHandler),
                methodName: nameof(StartTask),
                needStackTrace: true);
            curTask = null;
            return false;
        }

        branch.EffectTags.IncrementTagsValue(newTaskDef.effectFlags, addIfMiss: true);
        autoStartTaskChance = BaseAutoStartTaskChance(branch);
        if (newTaskDef == autoTargetTask)
        {
            ClearAutoTargetTask();
        }

        branch.MarkWorkStateDirty();
        return true;
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
            IEnumerable<BranchTaskDef> potentialTasks = FocusedTaskChivalry.AllBranchTasks;
            if (!potentialTasks.Any())
            {
                potentialTasks = DefDatabase<BranchTaskDef>.AllDefsListForReading.TakeRandom(20);
            }
            List<(BranchTaskDef task, float weight)> potentialTasksWeighter = [];
            foreach (BranchTaskDef task in potentialTasks)
            {
                if (!task.canBeRandomlyChosen)
                    continue;
                float weight = task.StartChecker?.RandomlyChosenWeight(branch) ?? -1f;
                if (weight > 0)
                {
                    potentialTasksWeighter.Add((task, weight));
                }
            }

            autoTargetTask = potentialTasksWeighter.RandomElementByWeightWithFallback(pair => pair.weight).task;
            autoTargetTask ??= BranchTaskDefOf.OARO_JurisdictionDuty;
        }

        if (Rand.Chance(usedChance) && autoTargetTask is not null)
        {
            if (TryStartTask(autoTargetTask))
            {
                ClearAutoTargetTask();
            }
            else
            {
                autoStartFailCount++;
            }
        }
    }

    private void ClearAutoTargetTask()
    {
        autoTargetTask = null;
        autoStartFailCount = 0;
        autoStartTaskChance = BaseAutoStartTaskChance(branch);
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