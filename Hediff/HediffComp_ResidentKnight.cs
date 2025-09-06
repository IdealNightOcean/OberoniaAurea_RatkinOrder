using Verse;

namespace OberoniaAurea.RatkinOrder;

public class HediffComp_ResidentKnight : HediffComp, ISingleRatkinOrderRelated
{
    private Map residentMap;
    private RatkinOrder ratkinOrder;
    public RatkinOrder RatkinOrder => ratkinOrder;

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_References.Look(ref residentMap, "residentMap");
        Scribe_References.Look(ref ratkinOrder, "ratkinOrder");
    }

    public void InitRatkinOrder(RatkinOrder ratkinOrder)
    {
        this.ratkinOrder = ratkinOrder;
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (this.ratkinOrder == ratkinOrder)
        {
            this.ratkinOrder = null;
        }
    }

    public override void Notify_PawnKilled()
    {
        OrderInteractionHandler.ResidentKnightHandler.RemoveResidentKnight(parent.pawn);
    }

    public override void Notify_Spawned()
    {
        OrderInteractionHandler.ResidentKnightHandler.AddNewResidentKnight(parent.pawn, ratkinOrder);
    }

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        OrderInteractionHandler.ResidentKnightHandler.RemoveResidentKnight(parent.pawn);
        residentMap = null;
    }
}
