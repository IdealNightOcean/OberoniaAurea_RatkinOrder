using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;


public class HeadSquad(SquadManager squadManager, bool initConstruction) : OrderSquad(squadManager, initConstruction)
{
    public override void UpdateStateDesc()
    {
        if (TaskState is not null)
        {
            return;
        }
        int hourOfDay = GenDate.HourOfDay(Find.TickManager.TicksAbs, 0f);
        if (hourOfDay <= 5 || hourOfDay >= 21)
        {
            state = "OARO_SquadStateRest".Translate();
            return;
        }

        state = "OARO_SquadStateIdle".Translate();
    }
}
