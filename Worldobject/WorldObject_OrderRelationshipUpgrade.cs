using Verse;

namespace OberoniaAurea.RatkinOrder;

public class WorldObject_OrderRelationshipUpgrade : WorldObject_InteractWithFixedCaravan_Nameable, ISingleRatkinOrderRelated
{
    private RatkinOrder ratkinOrder;
    public RatkinOrder RatkinOrder => ratkinOrder;
    public override int TicksNeeded => 15000;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref ratkinOrder, nameof(ratkinOrder));
    }

    public void InitRatkinOrder(RatkinOrder ratkinOrder)
    {
        this.ratkinOrder = ratkinOrder;
        if (this.ratkinOrder is not null)
        {
            Name = def.label + $" ({this.ratkinOrder.Name})";
        }
    }
    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (this.ratkinOrder == ratkinOrder)
        {
            this.ratkinOrder = null;
            InterruptWork();
        }
    }

    protected override void FinishWork()
    {
        if (ratkinOrder.IsValid())
        {
            this.SendWorkResolvedSignal();
        }
        this.SafeDestroy();
    }

    protected override void InterruptWork() => this.SafeDestroy();
}