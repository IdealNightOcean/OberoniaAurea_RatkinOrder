using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderRecommendation : ThingWithComps
{
    public void OnMakeForPlayer(RatkinOrder ratkinOrder)
    {
        if (ratkinOrder.IsValid())
        {
            ratkinOrder.EsteemHandler.TotalRecommendation += stackCount;
        }
    }
}