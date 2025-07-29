using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderRecommendation : ThingWithComps
{
    private RatkinOrder order;
    public RatkinOrder Order => order;

    public override string LabelNoCount
    {
        get
        {
            if (order is null)
            {
                return $"{base.LabelNoCount} ({"Invalid".Translate()})";
            }
            else
            {
                return $"{base.LabelNoCount} ({order.Name})";
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref order, "order");
    }

    public void SetRatkinOrder(RatkinOrder order)
    {
        this.order = order;
    }

    public void OnGiveToPlayer()
    {
        if (order is not null)
        {
            order.EsteemHandler.TotalRecommendation += stackCount;
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder removedOrder)
    {
        if (removedOrder == order)
        {
            order = null;
        }
    }

    public override bool CanStackWith(Thing other)
    {
        if (other is not OrderRecommendation otherRe || otherRe.order != order)
        {
            return false;
        }
        return base.CanStackWith(other);
    }
}