using OberoniaAurea_Frame;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_HonorPaladin : Hediff
{
    private int ticksToCheck = 600;

    private RangeHediffGiver_AddServity hediffGiver;
    public RangeHediffGiver_AddServity HediffGiver
    {
        get
        {
            if (hediffGiver is null)
                InitRangeHediffGiver();

            return hediffGiver;
        }
    }


    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksToCheck, nameof(ticksToCheck), defaultValue: 600);
    }

    public void InitRangeHediffGiver()
    {
        hediffGiver = new(linkedThing: pawn, hediffToGive: OARO_HediffDefOf.OARO_Hediff_HonorPaladin_Stimulate, radius: 3f)
        {
            TargetRace = RaceType.Humanlike,
            TargetRelation = TargetRelationType.NonHostile & ~TargetRelationType.Self,
            InitSeverity = 1f,
            AddSeverity = 1f
        };
    }

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if ((ticksToCheck -= delta) <= 0)
        {
            ticksToCheck = 600;
            if (pawn.Drafted && pawn.Spawned)
            {
                HediffGiver.Radius = 3f;
                HediffGiver.InitSeverity = 1f;
                HediffGiver.AddSeverity = 1f;
                HediffGiver.GiveHediffToRange();
            }
        }
    }

    public override void Notify_PawnKilled()
    {
        base.Notify_PawnKilled();
        if (pawn.MapHeld is not null)
        {
            HediffGiver.Radius = 5f;
            HediffGiver.InitSeverity = 5f;
            HediffGiver.AddSeverity = 5f;
            HediffGiver.GiveHediffToRange();
        }
    }
}