using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
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
        if (OrderHallHandler.OrderHallRoom is null)
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
    /// 能否申请新的常驻骑士
    /// </summary>
    public static AcceptanceReport CanApplyResidentKnight(RatkinOrder ratkinOrder, Map map, bool resultOnly)
    {
        if (map is null || ratkinOrder is null)
        {
            return false;
        }

        if (OrderHallHandler.OrderHallRoom is null)
        {
            return resultOnly ? false : "OARO_NoRatkinOrderHall".Translate();
        }

        if (ratkinOrder.Relationship < RelationshipKind.Friendly)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(RelationshipKind.Friendly.GetLabel());
        }

        int residentKnightCeiling = ResidentKnightsManager.ResidentKnightCeiling;
        if (ResidentKnightsManager.KnightsCount >= residentKnightCeiling)
        {
            return resultOnly ? false : "OARO_ReachMax_ResidentKnights".Translate(residentKnightCeiling);
        }
        int recommendationNeed = RecommendationUtility.RecommendationNeed_ApplyResidentKnight(ratkinOrder);
        if (RecommendationUtility.CurRecommendationOfMap(ratkinOrder, map) < recommendationNeed)
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(recommendationNeed, ratkinOrder.Name);
        }

        return true;
    }

    /// <summary>
    /// 申请新的常驻骑士
    /// </summary>
    public static void ApplyResidentKnight(RatkinOrder ratkinOrder, Map map)
    {
        Slate slate = new();
        slate.SetBasicOrderSlateVar(ratkinOrder);
        slate.Set("map", map);

        if (OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, OARO_QuestScriptDefOf.OARO_Quest_ResidentKnight, slate, forced: false))
        {
            int recommendationNeed = RecommendationUtility.RecommendationNeed_ApplyResidentKnight(ratkinOrder);
            if (recommendationNeed > 0)
            {
                RecommendationUtility.UseRecommendationOfMap(ratkinOrder, map, recommendationNeed);
            }
        }
    }

    public static AcceptanceReport CanUpgradeResidentKnightRank(ResidentKnightRecord record, Map map, bool resultOnly = false)
    {
        if (record.CurRank == ResidentKnightRecord.Rank.Crown)
        {
            return resultOnly ? false : "OARO_ReachMax_ResidentKnightRank".Translate();
        }

        int noAdditionalCostAcademicCeiling = record.NoAdditionalCostAcademicCeiling();
        if (record.TotalAcademicLevel < noAdditionalCostAcademicCeiling)
        {
            return resultOnly ? false : "OARO_Insufficient_TotalAcademicLevel".Translate(noAdditionalCostAcademicCeiling.ToString());
        }

        ResidentKnightRecord.Rank targetRank = ResidentKnightRecord.RankOffsetBy(record.CurRank, 1);
        RatkinOrder ratkinOrder = record.Branch.RatkinOrder;
        int recommendationNeed = RecommendationUtility.RecommendationNeed_ResidentKnightRankUpgrade(ratkinOrder, targetRank);
        if (recommendationNeed > 0 && RecommendationUtility.CurRecommendationOfMap(ratkinOrder, map) < recommendationNeed)
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(recommendationNeed, ratkinOrder.Name);
        }
        return true;
    }

    public static void UpgradeResidentKnightRank(Pawn knight, ResidentKnightRecord record, Map map)
    {
        ResidentKnightRecord.Rank targetRank = ResidentKnightRecord.RankOffsetBy(record.CurRank, 1);
        if (targetRank == record.CurRank)
        {
            return;
        }
        RatkinOrder ratkinOrder = record.Branch.RatkinOrder;

        int recommendationNeed = RecommendationUtility.RecommendationNeed_ResidentKnightRankUpgrade(ratkinOrder, targetRank);
        if (recommendationNeed > 0)
        {
            RecommendationUtility.UseRecommendationOfMap(ratkinOrder, map, recommendationNeed);
        }
        record.CurRank = targetRank;
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

        if (OrderHallHandler.OrderHallRoom is null)
        {
            return resultOnly ? false : "OARO_NoRatkinOrderHall".Translate();
        }

        if (knightGroup.RatkinOrder.Relationship <= RelationshipKind.Stranger)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(RelationshipKind.Friendly.GetLabel());
        }

        if (AroundKnightGroupsManager.SeasonInvitationUsed >= SeasonInvitationLimit())
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
        if (Rand.Chance(chance) && AroundKnightGroupsManager.TriggerVisitQuest(knightGroup, map))
        {
            AroundKnightGroupsManager.SeasonInvitationUsed++;
            if (AroundKnightGroupsManager.SeasonInvitationUsed > SeasonInvitationLimit())
            {
                RecommendationUtility.UseRecommendationOfMap(knightGroup.RatkinOrder, map, 1);
            }
        }
        else
        {
            AroundKnightGroupsManager.RemoveKnightGroup(knightGroup);
            AroundKnightGroupVisitInvalidDialog(knightGroup.Branch, isProactive: false);
        }
    }

    /// <summary>
    /// 小组到访失败行为
    /// 包括邀请失败和任务触发失败
    /// </summary>
    /// <param name="isProactive">是否为骑士小组主动</param>
    public static void AroundKnightGroupVisitInvalidDialog(Branch branch, bool isProactive)
    {
        GrammarRequest grammarRequest = new()
        {
            Includes = { OARO_ModDefOf.OARO_Dialog_AroundKnightGroupVisitInvalid }
        };
        grammarRequest.Rules.AddRange(ModUtility.RulesForRatkinOrder("ratkinOrder", branch.RatkinOrder));
        grammarRequest.Rules.AddRange(ModUtility.RulesForBranch("branch", branch, alsoAddOrderRule: false));
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

        stepChange = (OrderHallHandler.OrderHallLevel - 2) * 0.05f;
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
    /// 触发善行任务（实际前置任务）
    /// </summary>
    /// <param name="scriptDef">善行任务本体</param>
    /// <returns>是否成功触发</returns>
    public static bool TryTriggerMercyQuest(QuestScriptDef scriptDef)
    {
        Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
        if (map is null)
        {
            return false;
        }

        Slate slate = new();
        slate.Set("map", map);
        slate.Set(KeyLibrary_SlateStoreAs.MercyQuest, scriptDef);

        MercyQuestExtension mercyQuestExtension = scriptDef.GetModExtension<MercyQuestExtension>();
        if (mercyQuestExtension is null)
        {
            slate.Set(KeyLibrary_SlateStoreAs.SubFactionDef, OARO_ModDefOf.OARO_SubRakinia_Neutral);
            slate.Set(KeyLibrary_SlateStoreAs.HelpSeekerPawnKind, OARO_PawnKindDefOf.RatkinColonist);
        }
        else if (!mercyQuestExtension.TrySetQuestSlateValue(slate))
        {
            return false;
        }

        return OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(quest: out _,
                                                                     scriptDef: mercyQuestExtension?.preQuestDef ?? OARO_QuestScriptDefOf.OARO_MercyPre_HelpSeeker,
                                                                     slate: slate,
                                                                     forced: true);
    }


    /// <summary>
    /// 当前季度无花费邀请骑士小组上限
    /// </summary>
    private static int SeasonInvitationLimit()
    {
        return OrderHallHandler.OrderHallLevel switch
        {
            < 2 => 0,
            2 => 1,
            < 5 => 2,
            5 => 3,
            _ => 4
        };
    }
}