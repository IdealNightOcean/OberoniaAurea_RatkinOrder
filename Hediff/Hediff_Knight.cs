using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_Knight : HediffWithComps
{
    private KnightRecord knightRecord = new();

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

    public void InitKnightHediff(RatkinOrder ratkinOrder, Branch branch = null, bool isCommander = false)
    {
        if (ratkinOrder is null || !pawn.CanBeKnight())
        {
            pawn.health.RemoveHediff(this);
            return;
        }

        Log.Message($"Hediff_Knight InitRatkinOrder {pawn.Name}");
        knightRecord.RatkinOrder = ratkinOrder;
        knightRecord.Branch = branch;
        knightRecord.IsCommander = isCommander;
        KnightPawnsManager.RegisterKnight(pawn, knightRecord);
    }

    public void InitKnightHediff(Branch branch, bool isCommander = false) => InitKnightHediff(branch?.RatkinOrder, branch, isCommander);

    public override void PostRemoved()
    {
        Log.Message($"Hediff_Knight PostRemoved {pawn.Name}");
        KnightPawnsManager.DeregisterKnight(pawn);
        base.PostRemoved();
    }
}