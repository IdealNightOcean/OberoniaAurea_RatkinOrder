using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_GiveHediffInRange_KillHostile : KnightVirtueComp_GiveHediffInRange
{
    public override bool HasExtraPawnValiator => false;

    public override void Notify_KilledPawn(Pawn victim, DamageInfo? dinfo)
    {
        base.Notify_KilledPawn(victim, dinfo);
        if (victim.HostileTo(this.Pawn))
            HediffGiver.GiveHediffToRange();
    }
}
