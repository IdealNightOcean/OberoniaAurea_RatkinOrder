using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_HonorPaladin : Hediff
{
    private int ticksToCheck = 600;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksToCheck, "ticksToCheck", 600);
    }

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if ((ticksToCheck -= delta) <= 0)
        {
            ticksToCheck = 600;
            if (pawn.Drafted && pawn.Spawned)
            {
                AddStimulate(9f, 1f);
            }
        }
    }

    public override void Notify_PawnKilled()
    {
        base.Notify_PawnKilled();
        if (pawn.MapHeld is not null)
        {
            AddStimulate(25f, 5f);
        }
    }

    private void AddStimulate(float radiusSquared, float addSeverity)
    {
        IntVec3 pawnPos = pawn.PositionHeld;
        Faction faction = pawn.Faction ?? Faction.OfPlayer;

        foreach (Pawn p in pawn.MapHeld.mapPawns.AllPawnsSpawned)
        {
            if (!p.RaceProps.Humanlike || p.Position.DistanceToSquared(pawnPos) >= radiusSquared || p.HostileTo(faction))
            {
                continue;
            }

            if (p != pawn)
            {
                Hediff hediff = p.health.hediffSet.GetFirstHediffOfDef(OARO_HediffDefOf.OARO_Hediff_HonorPaladin_Stimulate);
                if (hediff is null)
                {
                    hediff = HediffMaker.MakeHediff(def, pawn);
                    hediff.Severity = addSeverity;
                    p.health.AddHediff(hediff);
                }
                else
                {
                    hediff.Severity += addSeverity;
                }
            }
        }
    }
}