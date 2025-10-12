using System;
using System.Reflection;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class SquadTask : IExposable
{
    private SquadTaskDef def;
    public SquadTaskDef Def => def;

    protected SquadTask() { }

    /// <summary>
    /// 常用于反射构造，注意子类同参数构造函数需要非公开
    /// </summary>
    protected SquadTask(SquadTaskDef def) => this.def = def;

    public virtual int TaskDurationTick(Squad squad)
    {
        return (int)(def.taskDurationDays * 60000f);
    }

    public virtual int SquadRestTick(Squad squad, bool interrupt)
    {
        return (int)(def.squadRestDays * 60000f);
    }

    public virtual void TickHour(Squad squad) { }
    public virtual void TaskStart(Squad squad) { }
    public virtual void TaskEnd(Squad squad, bool interrupt) { }

    public static SquadTask MakeTask(SquadTaskDef def)
    {
        return (SquadTask)Activator.CreateInstance(
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
