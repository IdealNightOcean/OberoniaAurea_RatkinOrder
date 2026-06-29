using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_GiveHediff_Self_KillHostile : KnightVirtueComp
{
    public KnightVirtueCompProperties_HediffGiver Props => (KnightVirtueCompProperties_HediffGiver)props;

    public override void Notify_KilledPawn(Pawn victim, DamageInfo? dinfo)
    {
        if (victim.HostileTo(Pawn))
            this.Pawn.GetOrAddHediff(Props.giveParams);
    }
}