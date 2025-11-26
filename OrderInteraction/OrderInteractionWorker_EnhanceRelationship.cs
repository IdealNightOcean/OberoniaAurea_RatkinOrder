using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderInteractionWorker_EnhanceRelationship(OrderInteractionDef def) : OrderInteractionWorker(def)
{
    public override AcceptanceReport CanUseInteraction(RatkinOrder ratkinOrder, Map map, bool resultOnly)
    {
        return ratkinOrder.CanUpgradeRelationship(map, byPlayer: true, resultOnly: resultOnly);
    }

    protected override (bool succeeded, bool doPostApply) InteractionEffect(RatkinOrder ratkinOrder, Map map)
    {
        bool succeeded = ratkinOrder.UpgradeRelationshipByPlayer(map);
        return (succeeded, true);
    }
}