using OberoniaAurea_Frame;
using RimWorld;
using System.Runtime.CompilerServices;
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
    public static AcceptanceReport CanRecruitKnight(Pawn knight, Map map, bool resultOnly)
    {
        if (OrderStationHandler.Instance.OrderStationRoom is null)
        {
            return resultOnly ? false : "OARO_NoRatkinOrderStation".Translate();
        }
        if (!KnightPawnsManager.Instance.TryGetKnightRecord(knight, out KnightRecord kRecord))
        {
            return resultOnly ? false : "OARO_PawnIsNotKnight".Translate(knight.Named(KeyLibrary_FormatArgName.PAWN));
        }

        if (kRecord.RatkinOrder.Relationship < RelationshipKind.Trustworthy)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(RelationshipUtility.GetLabel(RelationshipKind.Trustworthy));
        }

        int needRecommendation = RecommendationUtility.RecommendationNeed_RecruitmentKnight(kRecord.RatkinOrder);
        if (RecommendationUtility.CurRecommendationCount(map) < needRecommendation)
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(needRecommendation.Named(KeyLibrary_FormatArgName.Count));
        }
        return true;
    }

    /// <summary>
    /// 招募骑士
    /// </summary>
    public static void RecruitmentKnight(Pawn pawn, Map map)
    {
        if (!KnightPawnsManager.Instance.TryGetKnightRecord(pawn, out KnightRecord kRecord))
        {
            Log.Error("[OARO] 尝试招募非骑士单位作为骑士。");
            return;
        }

        int needRecommendation = RecommendationUtility.RecommendationNeed_RecruitmentKnight(kRecord.RatkinOrder);

        RecommendationUtility.UseRecommendationOfPlayer(map, needRecommendation);
        OAFrame_PawnUtility.MakePawnJoinPlayer(pawn);
        pawn.RemoveFirstHediffOfDef(OARO_HediffDefOf.OARO_Hediff_RecruitKnight);
        ResidentPawnsManager.Instance.TryRegisterKnight(pawn, kRecord);
    }

    /// <summary>
    /// 能否提升常驻骑士阶位
    /// </summary>
    public static AcceptanceReport CanUpgradeResidentKnightRank(ResidentKnight record, Map map, bool resultOnly)
    {
        if (record.CurRank == ResidentKnightRank.Crown)
        {
            return resultOnly ? false : "OARO_ReachMax_ResidentKnightRank".Translate();
        }

        int noAdditionalCostAcademicCeiling = AcademicUtility.GetNoAdditionalCostAcademicCeiling(record.CurRank);
        if (record.AcademicHandler.TotalAcademicLevel.Value < noAdditionalCostAcademicCeiling)
        {
            return resultOnly ? false : "OARO_Insufficient_TotalAcademicLevel".Translate(noAdditionalCostAcademicCeiling.Named(KeyLibrary_FormatArgName.Count));
        }
        /*
        ResidentKnight.ResidentKnightRank targetRank = ResidentKnight.RankOffsetBy(record.CurRank, 1);
        RatkinOrder ratkinOrder = record.RatkinOrder;
        int recommendationNeed = RecommendationUtility.RecommendationNeed_ResidentKnightRankUpgrade(ratkinOrder, targetRank);
        if (recommendationNeed > 0 && RecommendationUtility.CurRecommendationCount(map) < recommendationNeed)
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(recommendationNeed.Named(KeyLibrary_FormatArgName.Count));
        }
        */
        return true;
    }

    public static void UpgradeResidentKnightRank(ResidentKnight record, Map map)
    {
        ResidentKnightRank targetRank = record.CurRank.OffsetBy(1);
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

    public static AcceptanceReport CanPostponeResidentKnightkResignation(ResidentKnight record, Map map, bool resultOnly)
    {
        if (record.ResignationTick >= Find.TickManager.TicksGame + 20 * 60000)
        {
            return resultOnly ? false : "OARO_EnoughResignationDaysLeft".Translate(20.ToString());
        }
        if (RecommendationUtility.CurRecommendationCount(map) < 1)
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(1.Named(KeyLibrary_FormatArgName.Count));
        }
        return true;
    }

    public static void PostponeResidentKnightkResignation(ResidentKnight record, Map map)
    {
        RecommendationUtility.UseRecommendationOfPlayer(map, 1);
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

        if (OrderStationHandler.Instance.OrderStationRoom is null)
        {
            return resultOnly ? false : "OARO_NoRatkinOrderStation".Translate();
        }

        if (knightGroup.RatkinOrder.Relationship <= RelationshipKind.Stranger)
        {
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(RelationshipKind.Friendly.GetLabel());
        }

        if (AroundKnightGroupsManager.Instance.SeasonInvitationUsed >= SeasonInvitationLimit())
        {
            if (RecommendationUtility.CurRecommendationCount(map) < 1)
            {
                return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(1.Named(KeyLibrary_FormatArgName.Count));
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
        int seasonInvitationLimit = SeasonInvitationLimit();
        TaggedString text = AroundKnightGroupsManager.Instance.SeasonInvitationUsed > seasonInvitationLimit ? "OARO_InviteAroundKnightGroup_ConfirmOverLimit".Translate(
                                                                                                                    knightGroup.Branch.NameColored.Named(OARO_KeyLibrary_FormatArgName.BranchName),
                                                                                                                    chance.ToStringPercent().Named(KeyLibrary_FormatArgName.Chance))
                                                                                                            : "OARO_InviteAroundKnightGroup_Confirm".Translate(
                                                                                                                knightGroup.Branch.NameColored.Named(OARO_KeyLibrary_FormatArgName.BranchName),
                                                                                                                chance.ToStringPercent().Named(KeyLibrary_FormatArgName.Chance));

        Dialog_NodeTreeWithRatkinOrderInfo nodeTree = OARO_WindowUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(
            text,
            knightGroup.RatkinOrder,
            acceptAction: Invite);

        Find.WindowStack.Add(nodeTree);

        void Invite()
        {
            if (Rand.Chance(chance) && AroundKnightGroupsManager.Instance.TryTriggerVisitQuest(knightGroup, map, removeWhenInvalid: true))
            {
                if (AroundKnightGroupsManager.Instance.SeasonInvitationUsed > SeasonInvitationLimit())
                {
                    RecommendationUtility.UseRecommendationOfPlayer(map, 1);
                }
                AroundKnightGroupsManager.Instance.SeasonInvitationUsed++;

            }
            else
            {
                AroundKnightGroupsManager.Instance.RemoveKnightGroup(knightGroup);
                AroundKnightGroupVisitInvalidDialog(knightGroup, isProactive: false);
            }
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
        grammarRequest.Rules.AddRange(ModUtility.RulesForRatkinOrder(OARO_KeyLibrary_FormatArgName.ORDER, branch.RatkinOrder));
        grammarRequest.Rules.AddRange(ModUtility.RulesForBranch(OARO_KeyLibrary_FormatArgName.BRANCH, branch, alsoAddOrderRule: false));
        grammarRequest.Constants.Add("isProactive", isProactive.ToString());
        TaggedString talkText = GrammarResolver.Resolve("r_text", grammarRequest);

        Find.WindowStack.Add(OARO_WindowUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(talkText, branch.RatkinOrder));
    }

    /// <summary>
    /// 邀请附近骑士小组到访成功率
    /// </summary>
    public static float InvitationAcceptanceChance(AroundKnightGroup knights, bool resultOnly, out string explain)
    {
        explain = string.Empty;
        if (!AroundKnightGroup.Validate(knights))
        {
            return 0f;
        }
        float curChance = 0f;

        StringBuilder sb = resultOnly ? null : new(128);
        RatkinOrder ratkinOrder = knights.RatkinOrder;

        float stepChange = (int)ratkinOrder.Relationship * 0.04f;
        if (stepChange != 0f)
            ApplyStepChange(stepChange, "OARO_ChangeOffset_Relationship");

        ApplyStepChange(ratkinOrder.Esteem * 0.01f, "OARO_ChangeOffset_Esteem");

        stepChange = knights.CurBusyLevel switch
        {
            AroundKnightGroup.BusyLevel.Leisure => 0.2f,
            AroundKnightGroup.BusyLevel.Busy => -0.2f,
            AroundKnightGroup.BusyLevel.VeryBusy => -0.6f,
            _ => 0f
        };
        if (stepChange != 0f)
            ApplyStepChange(stepChange, $"OARO_AroundKnights_{knights.CurBusyLevel}_Offset");

        if (knights.TravelTicks >= 60000)
            ApplyStepChange(-0.15f, "OARO_AroundKnights_TravelTimeTooLong");
        else if (knights.TravelTicks <= 30000)
            ApplyStepChange(0.1f, "OARO_AroundKnights_TravelTimeShort");

        stepChange = (OrderStationHandler.Instance.OrderStationLevel - 2) * 0.05f;
        if (stepChange > 0f)
            ApplyStepChange(stepChange, "OARO_ChangeOffset_OrderStationLevel");

        if (ratkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder))
        {
            curChance += 0.2f;
            if (!resultOnly)
                sb.AppendLine("OARO_ChangeOffset_Reformation".Translate(
                    OrderReformationDefOf.OARO_ReformationPlaceholder.Named(KeyLibrary_FormatArgName.DEF),
                    OAFrame_TextUtility.ColoredPercentNamedArgument(0.2f, KeyLibrary_FormatArgName.Offset, includeSign: true)
                    ).Colorize(Color.green));
        }

        if (knights.Branch.IsBranchOfType(Branch.BranchType.Friendly))
        {
            curChance += 0.25f;
            curChance *= 1.25f;

            if (!resultOnly)
            {
                sb.AppendLine("OARO_ChangeOffset_BranchTypeOf".Translate($"OARO_{Branch.BranchType.Friendly}".Translate(),
                                                                         0.25f.ToStringPercentSigned("0.##").Named(KeyLibrary_FormatArgName.Offset))
                                                              .Colorize(Color.green));

                sb.AppendLine("OARO_ChangeFactor_BranchTypeOf".Translate($"OARO_{Branch.BranchType.Friendly}".Translate(),
                                                                         1.25f.ToStringPercent("0.##").Named(KeyLibrary_FormatArgName.Factor))
                                                              .Colorize(Color.green));
            }
        }

        curChance = Mathf.Clamp01(curChance);
        if (!resultOnly)
        {
            sb.AppendLine("OARO_AroundKnights_InvitationAcceptanceChance".Translate(curChance.ToStringPercent()));
            explain = sb.ToString();
        }
        return curChance;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ApplyStepChange(float change, string reason)
        {
            curChance += change;
            if (!resultOnly)
                sb.AppendLine(reason.Translate(change.ToStringPercentSigned("0.##")).Colorize(change < 0f ? ColorLibrary.RedReadable : Color.green));
        }
    }

    /// <summary>
    /// 当前季度无花费邀请骑士小组上限
    /// </summary>
    public static int SeasonInvitationLimit()
    {
        return OrderStationHandler.Instance.OrderStationLevel switch
        {
            < 2 => 0,
            2 => 1,
            < 5 => 2,
            5 => 3,
            _ => 4
        };
    }
}