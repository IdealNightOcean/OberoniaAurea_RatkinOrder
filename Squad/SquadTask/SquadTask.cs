using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class SquadTask : IExposable
{
    private SquadTaskDef def;
    public SquadTaskDef Def => def;

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
        SquadTask task = (SquadTask)Activator.CreateInstance(def.taskClass);
        task.def = def;
        return task;
    }

    public virtual void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
    }
}
