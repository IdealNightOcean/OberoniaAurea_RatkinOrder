using OberoniaAurea_Frame;
using RimWorld;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderInteractionWorker_Sponsor(OrderInteractionDef def) : OrderInteractionWorker(def)
{
    public override AcceptanceReport CanUseInteraction(RatkinOrder ratkinOrder, Map map, bool resultOnly)
    {
        AcceptanceReport baseReport = base.CanUseInteraction(ratkinOrder, map, resultOnly);
        if (!baseReport)
        {
            return baseReport;
        }

        if (map.AmountSendableSilver() < 5000)
        {
            return false;
        }
        return true;
    }

    public override void InteractionEffect(RatkinOrder ratkinOrder, Map map)
    {
        FundHandler fundHandler = ratkinOrder.FundHandler;
        fundHandler.AddFundEvent(OrderFundEventDefOf.OARO_PlayerSponsor_Immediate);
        fundHandler.AddFundEvent(OrderFundEventDefOf.OARO_PlayerSponsor_ShortTerm);
        fundHandler.AddFundEvent(OrderFundEventDefOf.OARO_PlayerSponsor_LongTerm);

        if (!ratkinOrder.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.AnnualFirstSponsor))
        {
            ratkinOrder.CooldownManager.RegisterRecord(key: KeyLibrary_CDRecord.AnnualFirstSponsor,
                                                       cdTicks: (60 - GenDate.DayOfYear(GenTicks.TicksAbs, 0) * 60000),
                                                       shouldRemoveWhenExpired: true);

            RecommendationUtility.GiveRecommendationsToPlayer_Map(ratkinOrder, 1, map, spawnCell: null, drop: true);

        }

        GlobalOrderInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.SponsoredSilver, 5000, addIfMiss: true);

        throw new NotImplementedException();
    }
}