using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class SquadTaskDef : Def
{
    public enum TaskPriority : byte
    {
        Low,
        Medium,
        High,
        Urgency,
    }

    private static readonly Type DefaultStartCheckerClass = typeof(SquadTaskStartChecker);
    private static readonly SquadTaskStartChecker DefaultStartChecker = new();

    public Type taskClass = typeof(SquadTask);
    public Type startCheckerClass = DefaultStartCheckerClass;
    [Unsaved] private SquadTaskStartChecker startChecker;
    public SquadTaskStartChecker StartChecker => startChecker ??= (startCheckerClass == DefaultStartCheckerClass) ? DefaultStartChecker : (SquadTaskStartChecker)Activator.CreateInstance(startCheckerClass);

    public TaskPriority priority = TaskPriority.Low;

    public bool canInterrupt = true;
    public bool ignoreRest = false; //是否无视休息
    public bool isOutdoor = false; //是否为户外任务

    public bool blockSupport = false; //是否阻止支援
    public bool blockBombard = false; //是否阻止炮击
    public bool blockRecover = false; //是否阻止恢复

    public bool canBeRandomlyChosen = true;


    public float taskDurationDays = 1f; //任务持续时间，单位为天（60000 tick = 1 天）
    public float squadRestDays; //任务结束后分队休息时间，单位为天（60000 tick = 1 天）
    public SquadTaskDef nextTaskStatus;

}