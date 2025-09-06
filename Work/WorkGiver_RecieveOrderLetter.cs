using OberoniaAurea_Frame;
using RimWorld;
using Verse;
using Verse.AI;

namespace OberoniaAurea.RatkinOrder;

public class WorkGiver_RecieveOrderLetter : WorkGiver_ThingDefScanner
{
    public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        return !OrderLetterBox.Instance.HasUnreadLetters;
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Building_OrderLetterBox)
        {
            return false;
        }
        if (!OrderLetterBox.Instance.HasUnreadLetters)
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
        return JobMaker.MakeJob(WorkThingDefRequest.jobDef, t);
    }
}
