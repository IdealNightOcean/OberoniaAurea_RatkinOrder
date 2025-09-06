using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace OberoniaAurea.RatkinOrder;

public class WorkGiver_TakeProductOutOfFermentingBarrel : WorkGiver_ThingDefScanner
{
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        List<Thing> barrels = pawn.Map.listerThings.ThingsOfDef(WorkThingDefRequest.thingDef);
        foreach (Thing barrel in barrels)
        {
            if (((Building_OrderFermentingBarrel)barrel).Fermented)
            {
                return false;
            }
        }
        return true;
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!(t is Building_OrderFermentingBarrel { Fermented: not false }))
        {
            return false;
        }
        if (t.IsBurning())
        {
            return false;
        }
        if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
        {
            return false;
        }
        return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return JobMaker.MakeJob(OARO_JobDefOf.OARO_TakeProductOutOfFermentingBarrel, t);
    }
}
