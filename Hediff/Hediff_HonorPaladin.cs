using OberoniaAurea_Frame;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_HonorPaladin : Hediff
{
    private int ticksToCheck = 600;

    private RangeHediffGiver hediffGiver;
    public RangeHediffGiver HediffGiver
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
        RangeHediffGiveParams giveParms = new(OARO_HediffDefOf.OARO_Hediff_HonorPaladin_Stimulate, 3f)
        {
            TargetRace = RaceType.Humanlike,
            TargetRelation = TargetRelationType.NonHostile & ~TargetRelationType.Self,
            InitSeverity = 1f,
            AddSeverityIfExist = 1f
        };
        hediffGiver = new(linkedThing: pawn, giveParms);
    }

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if ((ticksToCheck -= delta) <= 0)
        {
            ticksToCheck = 600;
            if (pawn.Drafted && pawn.Spawned)
            {
                HediffGiver.Parms.Radius = 3f;
                HediffGiver.Parms.InitSeverity = 1f;
                HediffGiver.Parms.AddSeverityIfExist = 1f;
                HediffGiver.GiveHediffToRange();
            }
        }
    }

    public override void Notify_PawnKilled()
    {
        base.Notify_PawnKilled();
        if (pawn.MapHeld is not null)
        {
            HediffGiver.Parms.Radius = 5f;
            HediffGiver.Parms.InitSeverity = 5f;
            HediffGiver.Parms.AddSeverityIfExist = 5f;
            HediffGiver.GiveHediffToRange();
        }
    }
}