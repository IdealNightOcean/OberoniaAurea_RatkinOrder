using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderInteractionWorker_MediateFactionRelation(OrderInteractionDef def) : OrderInteractionWorker(def)
{
    public override AcceptanceReport CanUseInteraction(RatkinOrder ratkinOrder, Map map, bool resultOnly)
    {
        if (ratkinOrder.Faction.PlayerRelationKind != FactionRelationKind.Neutral)
        {
            return "OARO_OrderFaction_NotNeutral".Translate();
        }
        if (ratkinOrder.Faction.PlayerGoodwill >= 0)
        {
            return "OARO_OrderFaction_NonNegativeGoodWill".Translate();
        }
        return base.CanUseInteraction(ratkinOrder, map, resultOnly);
    }

    protected override void InteractionEffect(RatkinOrder ratkinOrder, Map map)
    {
        ratkinOrder.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -ratkinOrder.Faction.PlayerGoodwill, reason: OARO_ModDefOf.OARO_OrderMediateFactionRelation);
    }
}
