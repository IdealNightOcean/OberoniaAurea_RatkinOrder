using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class GlobalOrderInteractionUtility
{
    /// <summary>
    /// 能否招募骑士
    /// </summary>
    public static AcceptanceReport CanRecruitKnight(RatkinOrder ratkinOrder, Map map, bool resultOnly)
    {
        if (GlobalOrderInteractionManager.RatkinOrderHall is null)
        {
            return resultOnly ? false : "OARO_NoRatkinOrderHall".Translate();
        }

        if (ratkinOrder.Relationship < OrderRelationshipKind.Trustworthy)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(RelationshipUtility.GetLabel(OrderRelationshipKind.Trustworthy));
        }

        int needRecommendation = ratkinOrder.Esteem switch
        {
            < 30 => 5,
            < 70 => 4,
            < 90 => 3,
            _ => 2
        };
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
        int needRecommendation = ratkinOrder.Esteem switch
        {
            < 30 => 5,
            < 70 => 4,
            < 90 => 3,
            _ => 2
        };

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

        if (GlobalOrderInteractionManager.RatkinOrderHall is null)
        {
            return resultOnly ? false : "OARO_NoRatkinOrderHall".Translate();
        }

        if (ratkinOrder.Relationship < OrderRelationshipKind.Friendly)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(OrderRelationshipKind.Friendly.GetLabel());
        }

        if (GlobalOrderInteractionManager.ResidentKnightsManager.ResidentKnights.Count >= GlobalOrderInteractionManager.ResidentKnightsManager.ResidentLimit)
        {
            return resultOnly ? false : "OARO_ReachMax_ResidentKnights".Translate(GlobalOrderInteractionManager.ResidentKnightsManager.ResidentLimit);
        }

        if (RecommendationUtility.CurRecommendationOfMap(ratkinOrder, map) < 1)
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(1, ratkinOrder.Name);
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

        OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, OARO_QuestScriptDefOf.OARO_Quest_ResidentKnight, slate, forced: false);
    }

    /// <summary>
    /// 常驻骑士额外上限 - 骑士团大厅
    /// </summary>
    public static int ExtraResidentKnightLimit_OrderHallLevel => GlobalOrderInteractionManager.OrderHallLevel switch
    {
        < 2 => 0,
        2 => 1,
        < 5 => 2,
        5 => 3,
        _ => 4
    };

    /// <summary>
    /// 当前季度无花费邀请骑士小组上限
    /// </summary>
    public static int SeasonInvitationLimit()
    {
        return GlobalOrderInteractionManager.OrderHallLevel switch
        {
            < 2 => 0,
            2 => 1,
            < 5 => 2,
            5 => 3,
            _ => 4
        };
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

        if (GlobalOrderInteractionManager.RatkinOrderHall is null)
        {
            return resultOnly ? false : "OARO_NoRatkinOrderHall".Translate();
        }

        if (knightGroup.RatkinOrder.Relationship <= OrderRelationshipKind.Stranger)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(OrderRelationshipKind.Friendly.GetLabel());
        }

        if (GlobalOrderInteractionManager.AroundKnightGroupsManager.SeasonInvitationUsed >= SeasonInvitationLimit())
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
        if (Rand.Chance(chance) && GlobalOrderInteractionManager.AroundKnightGroupsManager.TriggerVisitQuest(knightGroup, map))
        {
            GlobalOrderInteractionManager.AroundKnightGroupsManager.SeasonInvitationUsed++;
            if (GlobalOrderInteractionManager.AroundKnightGroupsManager.SeasonInvitationUsed > SeasonInvitationLimit())
            {
                RecommendationUtility.UseRecommendationOfMap(knightGroup.RatkinOrder, map, 1);
            }
        }
        else
        {
            GlobalOrderInteractionManager.AroundKnightGroupsManager.RemoveKnightGroup(knightGroup);
            AroundKnightGroupVisitInvalid(knightGroup.Branch, isProactive: false);
        }
    }

    /// <summary>
    /// 小组到访失败行为
    /// 包括邀请失败和任务触发失败
    /// </summary>
    /// <param name="isProactive">是否为骑士小组主动</param>
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

        stepChange = (GlobalOrderInteractionManager.OrderHallLevel - 2) * 0.05f;
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
            slate.Set(KeyLibrary_SlateStoreAs.SubRatkinFactionDef, OARO_ModDefOf.OARO_Rakinia_Sub);
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
}