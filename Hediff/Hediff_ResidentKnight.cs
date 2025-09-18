using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_ResidentKnight : HediffWithComps, ISingleRatkinOrderRelated
{
    private RatkinOrder ratkinOrder;
    public RatkinOrder RatkinOrder => ratkinOrder;

    public override void ExposeData()
    {
        base.ExposeData();
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
        GlobalOrderInteractionManager.ResidentKnightsManager.RemoveResidentKnight(pawn);
    }

    public override void Notify_Spawned()
    {
        GlobalOrderInteractionManager.ResidentKnightsManager.AddNewResidentKnight(pawn, ratkinOrder);
    }

    public override void PostRemoved()
    {
        base.PostRemoved();
        GlobalOrderInteractionManager.ResidentKnightsManager.RemoveResidentKnight(pawn);
    }
}
