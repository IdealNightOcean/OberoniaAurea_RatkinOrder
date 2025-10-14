using System;
using System.Reflection;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchTask : IExposable
{
    private BranchTaskDef def;
    public BranchTaskDef Def => def;

    protected BranchTask() { }

    /// <summary>
    /// 常用于反射构造，注意子类同参数构造函数需要非公开
    /// </summary>
    protected BranchTask(BranchTaskDef def) => this.def = def;

    public virtual int TaskDurationTick(Branch branch)
    {
        return (int)(def.durationDays * 60000f);
    }

    public virtual int BranchRestTick(Branch branch, bool interrupt)
    {
        return (int)(def.restDays * 60000f);
    }

    public virtual void TickHour(Branch branch) { }
    public virtual void TaskStart(Branch branch) { }
    public virtual void TaskEnd(Branch branch, bool interrupt) { }

    public static BranchTask MakeTask(BranchTaskDef def)
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
        Scribe_Defs.Look(ref def, "def");
    }
}
