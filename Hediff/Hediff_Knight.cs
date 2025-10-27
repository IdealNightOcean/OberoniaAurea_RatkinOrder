using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_Knight : HediffWithComps
{
    private KnightRecord knightRecord;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref knightRecord, "ratkinOrder");

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (knightRecord is null || knightRecord.RatkinOrder is null)
            {
                pawn.health.RemoveHediff(this);
            }
            else
            {
                KnightPawnsManager.RegisterKnight(pawn, knightRecord);
            }
        }
    }

    public void InitKnightHediff(KnightRecord knightRecord)
    {
        if (knightRecord?.RatkinOrder is null || !pawn.CanBeKnight())
        {
            pawn.health.RemoveHediff(this);
            return;
        }

        this.knightRecord = knightRecord;
        KnightPawnsManager.RegisterKnight(pawn, knightRecord);
    }


    public override void PostRemoved()
    {
        Log.Message($"Hediff_Knight PostRemoved {pawn.Name}");
        KnightPawnsManager.DeregisterKnight(pawn);
        base.PostRemoved();
    }
}