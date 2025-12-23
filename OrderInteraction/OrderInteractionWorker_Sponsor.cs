using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderInteractionWorker_Sponsor(OrderInteractionDef def) : OrderInteractionWorker(def)
{
    protected override (bool succeeded, bool doPostApply) InteractionEffect(RatkinOrder ratkinOrder, Map map)
    {
        FundHandler fundHandler = ratkinOrder.FundHandler;
        fundHandler.AddFundEvent(OrderFundEventDefOf.OARO_PlayerSponsor_Immediate);
        fundHandler.AddFundEvent(OrderFundEventDefOf.OARO_PlayerSponsor_ShortTerm);
        fundHandler.AddFundEvent(OrderFundEventDefOf.OARO_PlayerSponsor_LongTerm);

        if (!ratkinOrder.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.AnnualFirstSponsor))
        {
            ratkinOrder.CooldownManager.RegisterRecord(key: KeyLibrary_CDRecord.AnnualFirstSponsor,
                                                       cdTicks: (60 - GenLocalDate.DayOfYear(map)) * 60000,
                                                       removeWhenExpired: true);

            RecommendationUtility.GiveRecommendationsToPlayer_Map(count: 1, map, ratkinOrder, dropPod: true);

        }

        GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.SponsoredSilver, Def.needSilver, addIfMiss: true);
        return (true, true);
    }
}