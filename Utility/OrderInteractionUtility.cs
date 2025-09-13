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

    /// <summary>
    /// 能否招募骑士
    /// </summary>
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

    public static int SeasonInvitationLimit()
    {
        return OrderInteractionHandler.OrderHallLevel switch
        {
            < 2 => 0,
            2 => 1,
            < 5 => 2,
            5 => 3,
            _ => 4
        };
    }

    public static AcceptanceReport CanInviteAroundKnightGroup(AroundKnightGroup knightGroup, Map map)
    {
        if (knightGroup is null)
        {
            return false;
        }

        if (knightGroup.RatkinOrder.Relationship <= OrderRelationshipKind.Stranger)
        {
            return false;
        }

        if (OrderInteractionHandler.AroundKnightGroupsManager.SeasonInvitationUsed >= SeasonInvitationLimit())
        {
            if (RecommendationUtility.CurRecommendationOfMap(knightGroup.RatkinOrder, map) < 1)
            {
                return false;
            }
        }

        return true;
    }

    public static void InviteAroundKnightGroup(AroundKnightGroup knightGroup, Map map)
    {
        float chance = InvitationAcceptanceChance(knightGroup, resultOnly: true, out _);
        if (Rand.Chance(chance) && OrderInteractionHandler.AroundKnightGroupsManager.TriggerVisitQuest(knightGroup, map))
        {
            OrderInteractionHandler.AroundKnightGroupsManager.SeasonInvitationUsed++;
            if (OrderInteractionHandler.AroundKnightGroupsManager.SeasonInvitationUsed > SeasonInvitationLimit())
            {
                RecommendationUtility.UseRecommendationOfMap(knightGroup.RatkinOrder, map, 1);
            }
        }
        else
        {
            OrderInteractionHandler.AroundKnightGroupsManager.RemoveKnightGroup(knightGroup);
            AroundKnightGroupVisitInvalid(knightGroup.Branch, isProactive: false);
        }
    }

    public static void AroundKnightGroupVisitInvalid(Branch branch, bool isProactive)
    {
        if (isProactive)
        {

        }
        else
        {

        }
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

        ApplyStepChange((int)ratkinOrder.Relationship * 0.04f, "OARO_ChangeOffset_Relationship");
        ApplyStepChange(ratkinOrder.Esteem * 0.01f, "OARO_ChangeOffset_Esteem");

        float stepChange = knights.CurBusyLevel switch
        {
            AroundKnightGroup.BusyLevel.Leisure => 0.2f,
            AroundKnightGroup.BusyLevel.Busy => -0.2f,
            AroundKnightGroup.BusyLevel.VeryBusy => -0.6f,
            _ => 0f
        };
        curChance += stepChange;
        if (stepChange != 0f && !resultOnly)
        {
            sb.AppendInNewLine($"OARO_AroundKnights_{knights.CurBusyLevel}_Offset".Translate().Colorize(Color.green));
        }

        if (knights.TravelTicks >= 60000)
        {
            ApplyStepChange(-0.15f, "OARO_AroundKnights_TravelTimeTooLong");
        }
        else if (knights.TravelTicks <= 30000)
        {
            ApplyStepChange(0.1f, "OARO_AroundKnights_TravelTimeShort");
        }

        stepChange = (OrderInteractionHandler.OrderHallLevel - 2) * 0.05f;
        if (stepChange > 0f)
        {
            ApplyStepChange(stepChange, "OARO_ChangeOffset_OrderHallLevel");
        }

        if (ratkinOrder.ReformationManager.HasReformation(null))
        {
            curChance += 0.2f;
            if (!resultOnly)
            {
                sb.AppendInNewLine("OARO_ChangeOffset_Reformation".Translate().Colorize(Color.green));
            }
        }

        if (knights.Branch.IsBranchOfType(BranchType.Friendly))
        {
            ApplyStepChange(0.25f, "OARO_ChangeOffset_FriendlyBranch");

            curChance *= 1.25f;
            sb.AppendInNewLine("OARO_ChangeFactor_FriendlyBranch".Translate(1.25f.ToStringPercent("F2")).Colorize(Color.green));
        }

        if (!resultOnly)
        {
            explain = sb.ToString();
        }
        return Mathf.Clamp01(curChance);

        void ApplyStepChange(float change, string reason)
        {
            curChance += change;
            if (!resultOnly)
            {
                sb.AppendInNewLine(reason.Translate(change.ToStringPercent("F2")).Colorize(change < 0f ? ColorLibrary.RedReadable : Color.green));
            }
        }
    }
}