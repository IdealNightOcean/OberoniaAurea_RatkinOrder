using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_Knight : HediffWithComps, ISingleRatkinOrderRelated
{
    private RatkinOrder ratkinOrder;
    public RatkinOrder RatkinOrder => ratkinOrder;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref ratkinOrder, "ratkinOrder");

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (ratkinOrder is null)
            {
                pawn.health.RemoveHediff(this);
            }
            else
            {
                GameComponent_RatkinOrder.Instance.KnightPawns.Add(pawn);
            }
        }
    }

    public void InitRatkinOrder(RatkinOrder ratkinOrder)
    {
        this.ratkinOrder = ratkinOrder;
        Log.Message($"Hediff_Knight InitRatkinOrder {pawn.Name}");
        if (ratkinOrder is null)
        {
            pawn.health.RemoveHediff(this);
        }
        else
        {
            GameComponent_RatkinOrder.Instance.KnightPawns.Add(pawn);
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (this.ratkinOrder == ratkinOrder)
        {
            pawn.health.RemoveHediff(this);
        }
    }

    public override void Notify_PawnKilled()
    {
        base.Notify_PawnKilled();
    }

    override public void PostRemoved()
    {
        Log.Message($"Hediff_Knight PostRemoved {pawn.Name}");
        GameComponent_RatkinOrder.Instance.KnightPawns.Remove(pawn);
        base.PostRemoved();
    }
}