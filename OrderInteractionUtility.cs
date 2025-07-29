using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class OrderInteractionUtility
{
    public static void SponsorOrder(RatkinOrder order)
    {

        FundHandler fundHandler = order.FundHandler;
        fundHandler.AdjustFundsImmediately(0.015f);
        fundHandler.AddFundEvent(change: 0.005f, durationDays: 3, type: FundEvent.FundEvenType.Sponsor);
        fundHandler.AddFundEvent(change: 0.0025f, durationDays: 6, type: FundEvent.FundEvenType.Sponsor);
    }

    public static void RecruitmentKnight(RatkinOrder order, Pawn pawn)
    {
        int needRecommendation = GetRecommendationCountForRecruitment(order);

    }

    public static AcceptanceReport CanRecruitKnight(RatkinOrder order, bool resultOnly = false)
    {
        if (order.Relationship < EsteemHandler.RelationshipKind.Trustworthy)
        {
            return resultOnly ? false : "OARO_RefuseRecruitKnight_Relationship".Translate(EsteemUtility.GetRelationshipKindLabel(EsteemHandler.RelationshipKind.Trustworthy));
        }
        int needRecommendation = GetRecommendationCountForRecruitment(order);
        if (order.CurRecommendation < needRecommendation)
        {
            return resultOnly ? false : "OARO_RefuseRecruitKnight_Recommendation".Translate(needRecommendation);
        }
        return true;
    }

    private static int GetRecommendationCountForRecruitment(RatkinOrder order)
    {
        float esteem = order.Esteem;
        if (esteem < 0.3f)
        {
            return 5;
        }
        else if (esteem < 0.7f)
        {
            return 4;
        }
        else if (esteem < 0.9f)
        {
            return 3;
        }
        else
        {
            return 2;
        }
    }
}
