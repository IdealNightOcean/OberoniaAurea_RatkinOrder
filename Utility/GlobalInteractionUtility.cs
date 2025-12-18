using RimWorld;
using System;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Grammar;
using static OberoniaAurea.RatkinOrder.EsteemHandler;

namespace OberoniaAurea.RatkinOrder;

public static class GlobalInteractionUtility
{
    /// <summary>
    /// 能否招募骑士
    /// </summary>
    public static AcceptanceReport CanRecruitKnight(RatkinOrder ratkinOrder, Map map, bool resultOnly)
    {
        if (OrderHallHandler.Instance.OrderHallRoom is null)
        {
            return resultOnly ? false : "OARO_NoRatkinOrderHall".Translate();
        }

        if (ratkinOrder.Relationship < RelationshipKind.Trustworthy)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(RelationshipUtility.GetLabel(RelationshipKind.Trustworthy));
        }

        int needRecommendation = RecommendationUtility.RecommendationNeed_RecruitmentKnight(ratkinOrder);
        if (RecommendationUtility.CurRecommendationOfMap(ratkinOrder, map) < needRecommendation)
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(needRecommendation, ratkinOrder.Name);
        }
        return true;
    }

    /// <summary>
    /// 招募骑士
    /// </summary>
    public static void RecruitmentKnight(RatkinOrder ratkinOrder, Map map, Pawn pawn)
    {
        int needRecommendation = RecommendationUtility.RecommendationNeed_RecruitmentKnight(ratkinOrder);

        RecommendationUtility.UseRecommendationOfMap(ratkinOrder, map, needRecommendation);
        throw new NotImplementedException();

    }

    /// <summary>
    /// 能否提升常驻骑士阶位
    /// </summary>
    public static AcceptanceReport CanUpgradeResidentKnightRank(ResidentKnightRecord record, Map map, bool resultOnly)
    {
        if (record.CurRank == ResidentKnightRecord.Rank.Crown)
        {
            return resultOnly ? false : "OARO_ReachMax_ResidentKnightRank".Translate();
        }

        int noAdditionalCostAcademicCeiling = ResidentKnightRecord.GetNoAdditionalCostAcademicCeiling(record.CurRank);
        if (record.TotalAcademicLevel.Value < noAdditionalCostAcademicCeiling)
        {
            return resultOnly ? false : "OARO_Insufficient_TotalAcademicLevel".Translate(noAdditionalCostAcademicCeiling.Named(KeyLibrary_FormatArgName.Count));
        }
        /*
        ResidentKnightRecord.Rank targetRank = ResidentKnightRecord.RankOffsetBy(record.CurRank, 1);
        RatkinOrder ratkinOrder = record.RatkinOrder;
        int recommendationNeed = RecommendationUtility.RecommendationNeed_ResidentKnightRankUpgrade(ratkinOrder, targetRank);
        if (recommendationNeed > 0 && RecommendationUtility.CurRecommendationOfMap(ratkinOrder, map) < recommendationNeed)
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(recommendationNeed, ratkinOrder.Name);
        }
        */
        return true;
    }

    public static void UpgradeResidentKnightRank(ResidentKnightRecord record, Map map)
    {
        ResidentKnightRecord.Rank targetRank = ResidentKnightRecord.RankOffsetBy(record.CurRank, 1);
        if (targetRank == record.CurRank)
        {
            return;
        }
        /*
        RatkinOrder ratkinOrder = record.Branch.RatkinOrder;

        
        int recommendationNeed = RecommendationUtility.RecommendationNeed_ResidentKnightRankUpgrade(ratkinOrder, targetRank);
        if (recommendationNeed > 0)
        {
            RecommendationUtility.UseRecommendationOfMap(ratkinOrder, map, recommendationNeed);
        }
        */
        record.CurRank = targetRank;
    }

    public static AcceptanceReport CanPostponeResidentKnightkResignation(ResidentKnightRecord record, Map map, bool resultOnly)
    {
        if (record.ResignationTick >= Find.TickManager.TicksGame + 20 * 60000)
        {
            return resultOnly ? false : "OARO_EnoughResignationDaysLeft".Translate(20.ToString());
        }
        if (RecommendationUtility.CurRecommendationOfMap(record.RatkinOrder, map) < 1)
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(1, record.RatkinOrder.Name);
        }
        return true;
    }

    public static void PostponeResidentKnightkResignation(ResidentKnightRecord record, Map map)
    {
        RecommendationUtility.UseRecommendationOfMap(record.RatkinOrder, map, 1);
        record.PostponeResignation(120);
    }


    /// <summary>
    /// 能否邀请附近骑士小组到访
    /// </summary>
    public static AcceptanceReport CanInviteAroundKnightGroup(AroundKnightGroup knightGroup, Map map, bool resultOnly)
    {
        if (knightGroup is null || map is null)
        {
            return false;
        }

        if (OrderHallHandler.Instance.OrderHallRoom is null)
        {
            return resultOnly ? false : "OARO_NoRatkinOrderHall".Translate();
        }

        if (knightGroup.RatkinOrder.Relationship <= RelationshipKind.Stranger)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(RelationshipKind.Friendly.GetLabel());
        }

        if (AroundKnightGroupsManager.Instance.SeasonInvitationUsed >= SeasonInvitationLimit())
        {
            if (RecommendationUtility.CurRecommendationOfMap(knightGroup.RatkinOrder, map) < 1)
            {
                return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(1, knightGroup.RatkinOrder.Name);
            }
        }

        return true;
    }

    /// <summary>
    /// 玩家邀请附近骑士小组到访
    /// </summary>
    public static void InviteAroundKnightGroup(AroundKnightGroup knightGroup, Map map)
    {
        float chance = InvitationAcceptanceChance(knightGroup, resultOnly: true, out _);
        if (Rand.Chance(chance) && AroundKnightGroupsManager.Instance.TriggerVisitQuest(knightGroup, map))
        {
            AroundKnightGroupsManager.Instance.SeasonInvitationUsed++;
            if (AroundKnightGroupsManager.Instance.SeasonInvitationUsed > SeasonInvitationLimit())
            {
                RecommendationUtility.UseRecommendationOfMap(knightGroup.RatkinOrder, map, 1);
            }
        }
        else
        {
            AroundKnightGroupsManager.Instance.RemoveKnightGroup(knightGroup);
            AroundKnightGroupVisitInvalidDialog(knightGroup, isProactive: false);
        }
    }

    /// <summary>
    /// 小组到访失败行为
    /// 包括邀请失败和任务触发失败
    /// </summary>
    /// <param name="isProactive">是否为骑士小组主动</param>
    public static void AroundKnightGroupVisitInvalidDialog(AroundKnightGroup knightGroup, bool isProactive)
    {
        Branch branch = knightGroup.Branch;

        GrammarRequest grammarRequest = new()
        {
            Includes = { OARO_RulePackDefOf.OARO_Dialog_AroundKnightGroupVisitInvalid }
        };
        grammarRequest.Rules.AddRange(ModUtility.RulesForRatkinOrder(KeyLibrary_FormatArgName.ORDER, branch.RatkinOrder));
        grammarRequest.Rules.AddRange(ModUtility.RulesForBranch(KeyLibrary_FormatArgName.BRANCNH, branch, alsoAddOrderRule: false));
        grammarRequest.Constants.Add("isProactive", isProactive.ToString());
        TaggedString talkText = GrammarResolver.Resolve("r_text", grammarRequest);

        Find.WindowStack.Add(OARO_WindowUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(talkText, branch.RatkinOrder));
    }

    /// <summary>
    /// 邀请附近骑士小组到访成功率
    /// </summary>
    public static float InvitationAcceptanceChance(AroundKnightGroup knights, bool resultOnly, out string explain)
    {
        explain = null;
        if (!AroundKnightGroup.Validate(knights))
        {
            return 0f;
        }
        float curChance = 0f;

        StringBuilder sb = resultOnly ? null : new(128);
        RatkinOrder ratkinOrder = knights.RatkinOrder;

        float stepChange = (int)ratkinOrder.Relationship * 0.04f;
        if (stepChange != 0f)
        {
            ApplyStepChange(stepChange, "OARO_ChangeOffset_Relationship");
        }

        ApplyStepChange(ratkinOrder.Esteem * 0.01f, "OARO_ChangeOffset_Esteem");

        stepChange = knights.CurBusyLevel switch
        {
            AroundKnightGroup.BusyLevel.Leisure => 0.2f,
            AroundKnightGroup.BusyLevel.Busy => -0.2f,
            AroundKnightGroup.BusyLevel.VeryBusy => -0.6f,
            _ => 0f
        };
        if (stepChange != 0f)
        {
            ApplyStepChange(stepChange, $"OARO_AroundKnights_{knights.CurBusyLevel}_Offset");
        }

        if (knights.TravelTicks >= 60000)
        {
            ApplyStepChange(-0.15f, "OARO_AroundKnights_TravelTimeTooLong");
        }
        else if (knights.TravelTicks <= 30000)
        {
            ApplyStepChange(0.1f, "OARO_AroundKnights_TravelTimeShort");
        }

        stepChange = (OrderHallHandler.Instance.OrderHallLevel - 2) * 0.05f;
        if (stepChange > 0f)
        {
            ApplyStepChange(stepChange, "OARO_ChangeOffset_OrderHallLevel");
        }

        if (ratkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder))
        {
            curChance += 0.2f;
            if (!resultOnly)
            {
                sb.AppendInNewLine("OARO_ChangeOffset_Reformation".Translate().Colorize(Color.green));
            }
        }

        if (knights.Branch.IsBranchOfType(Branch.BranchType.Friendly))
        {
            ApplyStepChange(0.25f, "OARO_ChangeOffset_FriendlyBranch");

            curChance *= 1.25f;
            sb.AppendInNewLine("OARO_ChangeFactor_FriendlyBranch".Translate(1.25f.ToStringPercent("0.##")).Colorize(Color.green));
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
                sb.AppendInNewLine(reason.Translate(change.ToStringPercentSigned("0.##")).Colorize(change < 0f ? ColorLibrary.RedReadable : Color.green));
            }
        }
    }

    /// <summary>
    /// 当前季度无花费邀请骑士小组上限
    /// </summary>
    private static int SeasonInvitationLimit()
    {
        return OrderHallHandler.Instance.OrderHallLevel switch
        {
            < 2 => 0,
            2 => 1,
            < 5 => 2,
            5 => 3,
            _ => 4
        };
    }
}