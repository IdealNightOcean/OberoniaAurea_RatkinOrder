using OberoniaAurea.RatkinOrder.DataLibrary;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderRecommendation : ThingWithComps
{
    public void OnMakeForPlayer()
    {
        GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.TotalRecommendation, offset: stackCount, addIfMiss: true);
    }
}