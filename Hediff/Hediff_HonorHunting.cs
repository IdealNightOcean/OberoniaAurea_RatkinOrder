using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_HonorHunting : Hediff
{
    private int nextCanApplyTick = -1;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref nextCanApplyTick, "nextCanApplyTick", -1);
    }

    public override void Notify_PawnDamagedThing(Thing thing, DamageInfo dinfo, DamageWorker.DamageResult result)
    {
        base.Notify_PawnDamagedThing(thing, dinfo, result);
        if (Find.TickManager.TicksGame > nextCanApplyTick && thing is Pawn victim)
        {
            nextCanApplyTick = Find.TickManager.TicksGame + 1800;
            Hediff hediff = victim.health.hediffSet.GetFirstHediffOfDef(OARO_HediffDefOf.OARO_Hediff_HonorHunting_Debuff);
            if (hediff is null)
            {
                hediff = HediffMaker.MakeHediff(OARO_HediffDefOf.OARO_Hediff_HonorHunting_Debuff, pawn);
                hediff.Severity = 1f;
                victim.health.AddHediff(hediff);
            }
            else
            {
                hediff.Severity += 1f;
            }
        }
    }
}