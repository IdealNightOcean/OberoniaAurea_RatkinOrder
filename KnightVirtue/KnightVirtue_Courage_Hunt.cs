using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtue_Courage_Hunt : KnightVirtue
{
    protected HediffGiveParams giveParams;

    public override void Notify_KilledPawn(Pawn victim, DamageInfo? dinfo)
    {
        if (!victim.HostileTo(Pawn))
            return;

        giveParams ??= Def.GetModExtension<ModExtension_GiveHediff>()?.giveParams;
        this.Pawn.GetOrAddHediff(giveParams);
    }
}