using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class StatWorker_PawnVirtue : StatWorker
{
    protected static bool CanApplyOn(StatRequest req)
    {
        Pawn pawn = req.Pawn ?? (req.Thing as Pawn);
        if (pawn is null || !pawn.Faction.IsPlayerSafe())
        {
            return false;
        }

        return true;
    }

    protected static bool CanApplyOn(Thing thing)
    {
        if (thing is Pawn pawn)
        {
            return pawn.Faction.IsPlayerSafe();
        }
        return false;
    }

    public override bool ShouldShowFor(StatRequest req) => CanApplyOn(req) && base.ShouldShowFor(req);

    public override bool IsDisabledFor(Thing thing) => !CanApplyOn(thing) || base.IsDisabledFor(thing);

    public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
    {
        Pawn pawn = req.Pawn ?? (req.Thing as Pawn);

        if (pawn is null || !pawn.Faction.IsPlayerSafe())
        {
            return 0f;
        }


        return 0f;
    }
}
