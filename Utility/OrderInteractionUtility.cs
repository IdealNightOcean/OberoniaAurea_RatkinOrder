using RimWorld;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class OrderInteractionUtility
{
    public static void SponsorOrder(RatkinOrder order)
    {

        FundHandler fundHandler = order.FundHandler;
        fundHandler.AddFundEvent(OrderFundEventDefOf.OARO_PlayerSponsor_Immediate);
        fundHandler.AddFundEvent(OrderFundEventDefOf.OARO_PlayerSponsor_ShortTerm);
        fundHandler.AddFundEvent(OrderFundEventDefOf.OARO_PlayerSponsor_LongTerm);

        if (!order.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.AnnualFirstSponsor))
        {
            order.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.AnnualFirstSponsor,
                                                 cdTicks: (60 - GenDate.DayOfYear(GenTicks.TicksAbs, 0) * 60000),
                                                 shouldRemoveWhenExpired: true);
        }

        throw new NotImplementedException();
    }

    public static void RecruitmentKnight(RatkinOrder order, Map map, Pawn pawn)
    {
        int needRecommendation = order.Esteem switch
        {
            < 30 => 5,
            < 70 => 4,
            < 90 => 3,
            _ => 2
        };

        RecommendationUtility.UseRecommendationOfMap(order, map, needRecommendation);
        throw new NotImplementedException();

    }

    public static AcceptanceReport CanRecruitKnight(RatkinOrder order, Map map, bool resultOnly)
    {
        if (order.Relationship < OrderRelationshipKind.Trustworthy)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(EsteemUtility.GetRelationshipKindLabel(OrderRelationshipKind.Trustworthy));
        }

        int needRecommendation = order.Esteem switch
        {
            < 30 => 5,
            < 70 => 4,
            < 90 => 3,
            _ => 2
        };
        if (RecommendationUtility.CurRecommendationOfMap(order, map) < needRecommendation)
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(needRecommendation);
        }
        return true;
    }
}
