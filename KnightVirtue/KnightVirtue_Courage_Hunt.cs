using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtue_Courage_Hunt : KnightVirtue
{
    public override void Notify_KilledPawn(Pawn victim, DamageInfo? dinfo)
    {
        if (!victim.HostileTo(Pawn))
            return;

        HediffGiveParams giveParams = Def.GetModExtension<ModExtension_GiveHediff>()?.giveParams;
        OAFrame_PawnUtility.GetOrAddHediffToPawn(knight.Pawn, giveParams);
    }
}