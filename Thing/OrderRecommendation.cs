using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderRecommendation : ThingWithComps
{
    private RatkinOrder ratkinOrder;
    public RatkinOrder RatkinOrder => ratkinOrder;

    public override string LabelNoCount
    {
        get
        {
            if (!ratkinOrder.IsValid())
            {
                return $"{base.LabelNoCount} ({"Invalid".Translate()})";
            }
            else
            {
                return $"{base.LabelNoCount} ({ratkinOrder.Name})";
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref ratkinOrder, "ratkinOrder");
    }

    public void SetRatkinOrder(RatkinOrder order)
    {
        ratkinOrder = order;
    }

    public void OnGiveToPlayer()
    {
        if (ratkinOrder.IsValid())
        {
            ratkinOrder.EsteemHandler.TotalRecommendation += stackCount;
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder removedOrder)
    {
        if (removedOrder == ratkinOrder)
        {
            ratkinOrder = null;
        }
    }

    public override bool CanStackWith(Thing other)
    {
        if (other is not OrderRecommendation otherRe || otherRe.ratkinOrder != ratkinOrder)
        {
            return false;
        }
        return base.CanStackWith(other);
    }
}