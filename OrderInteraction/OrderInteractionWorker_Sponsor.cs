using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderInteractionWorker_Sponsor(OrderInteractionDef def) : OrderInteractionWorker(def)
{
    protected override void InteractionEffect(RatkinOrder ratkinOrder, Map map)
    {
        FundHandler fundHandler = ratkinOrder.FundHandler;
        fundHandler.AddFundEvent(OrderFundEventDefOf.OARO_PlayerSponsor_Immediate);
        fundHandler.AddFundEvent(OrderFundEventDefOf.OARO_PlayerSponsor_ShortTerm);
        fundHandler.AddFundEvent(OrderFundEventDefOf.OARO_PlayerSponsor_LongTerm);

        if (!ratkinOrder.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.AnnualFirstSponsor))
        {
            ratkinOrder.CooldownManager.RegisterRecord(key: KeyLibrary_CDRecord.AnnualFirstSponsor,
                                                       cdTicks: (60 - GenLocalDate.DayOfYear(map)) * 60000,
                                                       shouldRemoveWhenExpired: true);

            RecommendationUtility.GiveRecommendationsToPlayer_Map(ratkinOrder, 1, map, dropPod: true);

        }

        GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.SponsoredSilver, Def.needSilver, addIfMiss: true);
    }
}