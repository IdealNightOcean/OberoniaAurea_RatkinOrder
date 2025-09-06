using RimWorld;
using System;
using System.Text;
using UnityEngine;
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

    /// <summary>
    /// 邀请附近骑士小组到访成功率
    /// </summary>
    public static float InvitationAcceptanceChance(AroundKnightGroup knights, bool resultOnly, out string explain)
    {
        explain = null;
        if (AroundKnightGroup.Validate(knights))
        {
            return 0f;
        }
        float curChance = 0f;

        StringBuilder sb = resultOnly ? null : new();
        RatkinOrder ratkinOrder = knights.RatkinOrder;

        ApplyStepChange((int)ratkinOrder.Relationship * 0.04f, "OARO_AroundKnights_Relationship");

        ApplyStepChange(ratkinOrder.Esteem * 0.01f, "OARO_AroundKnights_Esteem");

        float stepChange = knights.CurBusyLevel switch
        {
            AroundKnightGroup.BusyLevel.Leisure => 0.2f,
            AroundKnightGroup.BusyLevel.Busy => -0.2f,
            AroundKnightGroup.BusyLevel.VeryBusy => -0.6f,
            _ => 0f
        };
        ApplyStepChange(stepChange, "OARO_AroundKnights_BusyLevel");


        if (knights.TravelTicks >= 60000)
        {
            stepChange = -0.15f;
            ApplyStepChange(stepChange, "OARO_AroundKnights_TravelTimeTooLong");

        }
        else if (knights.TravelTicks <= 30000)
        {
            stepChange = -0.15f;
            ApplyStepChange(stepChange, "OARO_AroundKnights_TravelTimeShort");
        }

        stepChange = (OrderInteractionHandler.OrderHallLevel - 2) * 0.05f;
        stepChange = stepChange > 0f ? stepChange : 0f;
        ApplyStepChange(stepChange, "OARO_AroundKnights_OrderHallLevel");

        if (ratkinOrder.ReformationManager.HasReformation(null))
        {
            ApplyStepChange(0.2f, "OARO_AroundKnights_Reformation");
        }

        if (knights.Branch.IsBranchOfType(BranchType.Friendly))
        {
            ApplyStepChange(0.25f, "OARO_AroundKnights_FriendlyBranch");
            curChance *= 1.25f;
            sb.AppendInNewLine("OARO_AroundKnights_FriendlyBranch_Multi".Translate(1.25f.ToStringPercent("F2")).Colorize(Color.green));
        }

        if (!resultOnly)
        {
            explain = sb.ToString();
        }
        return Mathf.Clamp01(curChance);

        void ApplyStepChange(float change, string reason)
        {
            if (change == 0f)
            {
                return;
            }

            curChance += change;
            if (!resultOnly)
            {
                sb.AppendInNewLine(reason.Translate(change.ToStringPercent("F2")).Colorize(change < 0f ? ColorLibrary.RedReadable : Color.green));
            }
        }
    }
}
