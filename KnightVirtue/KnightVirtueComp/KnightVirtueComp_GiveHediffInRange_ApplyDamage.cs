using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_GiveHediffInRange_ApplyDamage : KnightVirtueComp_GiveHediffInRange
{
    public override bool HasExtraPawnValiator => false;

    public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt) => HediffGiver.GiveHediffToRange();
}
