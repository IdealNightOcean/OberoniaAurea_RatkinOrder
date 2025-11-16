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

    public Type taskClass = typeof(BranchTask);
    public Type startCheckerClass = DefaultStartCheckerClass;
    [Unsaved] private BranchTaskStartChecker startChecker;
    public BranchTaskStartChecker StartChecker => startChecker ??= (startCheckerClass == DefaultStartCheckerClass) ? DefaultStartChecker : (BranchTaskStartChecker)Activator.CreateInstance(startCheckerClass);

    public TaskPriority priority;
    public BranchTaskType taskType;

    public bool isOutdoorTask; //是否为户外任务
    public bool canInterrupted = true;
    public bool canInterruptedByJointPatrol = true; //能否被边境轮巡打断
    public bool ignoreRest; //是否无视休息

    public bool canBeRandomlyChosen = true;

    public List<string> effectFlags; //效果标志列表

    public float durationDays = 1f; //任务持续时间，单位为天（60000 tick = 1 天）
    public float restDays; //任务结束后休息时间，单位为天（60000 tick = 1 天）
    public BranchTaskDef nextTask;
}