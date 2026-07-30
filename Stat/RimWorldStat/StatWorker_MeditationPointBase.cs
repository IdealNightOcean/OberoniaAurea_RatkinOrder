using OberoniaAurea.RatkinOrder.Utility;
using RimWorld;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class StatWorker_MeditationPointBase : StatWorker
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool CanApplyOn(StatRequest req)
    {
        Pawn pawn = req.Pawn ?? (req.Thing as Pawn);
        return pawn.CanBeKnight() && pawn.Faction.IsPlayerSafe() && ResidentPawnsManager.Instance.IsResidentKnight(pawn);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool CanApplyOn(Thing thing)
    {
        if (thing is Pawn pawn)
        {
            return pawn.CanBeKnight() && pawn.Faction.IsPlayerSafe() && ResidentPawnsManager.Instance.IsResidentKnight(pawn);
        }
        return false;
    }

    public override bool ShouldShowFor(StatRequest req) => CanApplyOn(req) && base.ShouldShowFor(req);

    public override bool IsDisabledFor(Thing thing) => !CanApplyOn(thing) && base.IsDisabledFor(thing);
}