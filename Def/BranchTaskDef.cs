using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchTaskDef : Def
{
    public enum TaskPriority : byte
    {
        Low,
        Medium,
        High,
        Urgency
    }

    private static readonly Type DefaultStartCheckerClass = typeof(BranchTaskStartChecker);
    private static readonly BranchTaskStartChecker DefaultStartChecker = new();

    /// <summary>
    /// 任务功能类
    /// </summary>
    public Type taskClass = typeof(BranchTask);

    /// <summary>任务开始检测类</summary>
    /// <remarks>
    /// <para>- 用于检测 <see cref="Branch"/> 是否可以开始进行该任务</para>
    /// <para>- 在<see cref="BranchTaskHandler"/> 的检测之后</para>
    /// <para>- 随机选择权重</para>
    /// </remarks>
    public Type startCheckerClass = DefaultStartCheckerClass;
    [Unsaved] private BranchTaskStartChecker startChecker;
    public BranchTaskStartChecker StartChecker => startChecker ??= (startCheckerClass == DefaultStartCheckerClass) ? DefaultStartChecker : (BranchTaskStartChecker)Activator.CreateInstance(startCheckerClass);

    /// <summary>
    /// 任务优先级
    /// </summary>
    public TaskPriority priority;

    /// <summary>
    /// 任务对应专注类型
    /// </summary>
    public BranchTaskType taskType;

    /// <summary>
    /// 是否为户外任务
    /// </summary>
    public bool isOutdoorTask;

    /// <summary>
    /// 是否有分险
    /// </summary>
    public bool hasRisk;

    /// <summary>
    /// 基础风险
    /// </summary>
    public float baseRiskProbability;

    /// <summary>
    /// 能否被打断
    /// </summary>
    public bool canInterrupted = true;

    /// <summary>
    /// 能否被边境轮巡打断
    /// </summary>
    public bool canInterruptedByJointPatrol = true;

    /// <summary>
    /// 是否无视休息
    /// </summary>
    public bool ignoreRest;

    /// <summary>
    /// 是否可被随机选择
    /// </summary>
    public bool canBeRandomlyChosen = true;

    /// <summary>
    /// 效果标志列表
    /// </summary>
    public List<string> effectFlags;

    /// <summary>
    /// 任务持续时间（Day）
    /// </summary>
    public float durationDays = 1f;

    /// <summary>
    /// 任务结束后休息时间（Day）
    /// </summary>
    public float restDays;

    /// <summary>
    /// 下一个任务，可为 <see langword="null"/>
    /// </summary>
    /// <remarks>
    /// <para>- 当前任务结束后将会尝试开始</para>
    /// </remarks>
    public BranchTaskDef nextTask;
}