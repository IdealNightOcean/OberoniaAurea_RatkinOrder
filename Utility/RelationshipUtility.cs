using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.EsteemHandler;

namespace OberoniaAurea.RatkinOrder;

public static class RelationshipUtility
{
    public const int RelationshipKindCount = 5;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color GetColor(this RelationshipKind relationship)
    {
        return relationship switch
        {
            RelationshipKind.Stranger => Color.white,
            RelationshipKind.Acquaintance => Color.cyan,
            RelationshipKind.Friendly => Color.green,
            RelationshipKind.Trustworthy => Color.green,
            RelationshipKind.Soulmate => Color.green,
            _ => Color.white
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetLabel(this RelationshipKind relationship)
    {
        return $"OARO_Relationship_{relationship}".Translate().Colorize(relationship.GetColor());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetDescription(RatkinOrder ratkinOrder, RelationshipKind relationship)
    {
        return $"OARO_RelationshipDesc_{relationship}".Translate(ratkinOrder.Name).Colorize(relationship.GetColor());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RelationshipKind RelationshipKindOffsetBy(this RelationshipKind relationship, int offset)
    {
        return (RelationshipKind)Mathf.Clamp((int)relationship + offset, 0, RelationshipKindCount - 1);
    }

    public static void RelationshipKindOffsetBy(this RatkinOrder ratkinOrder, int offset, string reason, bool sendLetter)
    {
        if (offset == 0)
        {
            return;
        }
        ratkinOrder.EsteemHandler.SetRelationship(RelationshipKindOffsetBy(ratkinOrder.Relationship, offset), reason, sendLetter);
    }

    /// <summary>
    /// 能否提升关系类型等级
    /// </summary>
    public static AcceptanceReport CanUpgradeRelationship(this RatkinOrder ratkinOrder, Map map, bool byPlayer, bool resultOnly)
    {
        RelationshipKind curRelationship = ratkinOrder.Relationship;
        if (curRelationship == RelationshipKind.Soulmate)
        {
            return resultOnly ? false : "OARO_ReachMax_OrderRelationship".Translate();
        }
        if (ratkinOrder.Faction.HostileTo(Faction.OfPlayer))
        {
            return resultOnly ? false : "OARO_OrderFaction_Hostile".Translate();
        }

        if (byPlayer)
        {
            if (ratkinOrder.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.RelationshipUpgraded))
            {
                return resultOnly ? false : "OARO_Cooling_RelationshipUpgraded".Translate();
            }
        }
        else
        {
            if (ratkinOrder.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.AutoRelationshipUpgraded))
            {
                return resultOnly ? false : "OARO_Cooling_AutoRelationshipUpgraded".Translate();
            }
        }

        RelationshipKind newRelationship = curRelationship.RelationshipKindOffsetBy(1);
        int curRecommendationNeed = RecommendationUtility.RecommendationNeed_OrderRelationUpgrade(newRelationship);

        switch (newRelationship)
        {
            case RelationshipKind.Acquaintance:
                return ValidateRelationshipRequirement(esteem: 20, totalRecommendation: 1);
            case RelationshipKind.Friendly:
                return ValidateRelationshipRequirement(esteem: 30, totalRecommendation: 3);
            case RelationshipKind.Trustworthy:
                if (ratkinOrder.BranchManager.NormalDemandFulfillCount < 2)
                {
                    return resultOnly ? false : "OARO_Insufficient_NormalDemandFulfill".Translate(2);
                }
                return ValidateRelationshipRequirement(esteem: 40, totalRecommendation: 6, friendlyBranchesCount: 1);

            case RelationshipKind.Soulmate:
                if (ratkinOrder.BranchManager.CriticalDemandFulfillCount < 2)
                {
                    return resultOnly ? false : "OARO_Insufficient_CriticalDemandFulfill".Translate(2);
                }
                return ValidateRelationshipRequirement(esteem: 50, totalRecommendation: 12, friendlyBranchesCount: 3);
            default:
                return true;
        }

        AcceptanceReport ValidateRelationshipRequirement(int esteem, int totalRecommendation, int friendlyBranchesCount = -1)
        {
            if (!byPlayer && ratkinOrder.Esteem < esteem)
            {
                return resultOnly ? false : "OARO_Insufficient_Esteem".Translate(esteem);
            }
            if (ratkinOrder.EsteemHandler.TotalRecommendation < totalRecommendation)
            {
                return resultOnly ? false : "OARO_Insufficient_TotalRecommendation".Translate(totalRecommendation, ratkinOrder.Name);
            }
            if (friendlyBranchesCount > 0 && ratkinOrder.BranchManager.FriendlyBranchesCount.Value < friendlyBranchesCount)
            {
                return resultOnly ? false : "OARO_Insufficient_FriendlyBranches".Translate(friendlyBranchesCount);
            }
            if (byPlayer && curRecommendationNeed > 0 && RecommendationUtility.CurRecommendationOfMap(ratkinOrder, map) < curRecommendationNeed)
            {
                return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(curRecommendationNeed, ratkinOrder.Name);
            }
            return true;
        }
    }

    /// <summary>
    /// 提升骑士团关系（玩家）
    /// </summary>
    public static bool UpgradeRelationshipByPlayer(this RatkinOrder ratkinOrder, Map map)
    {
        if (ratkinOrder.Relationship == RelationshipKind.Soulmate)
        {
            return false;
        }

        if (ratkinOrder.Relationship < RelationshipKind.Friendly)
        {
            ratkinOrder.RelationshipKindOffsetBy(1, "OARO_Relationship_PlayerUpgraded".Translate(), sendLetter: true);
            ratkinOrder.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.RelationshipUpgraded, cdTicks: 5 * 60000, removeWhenExpired: true);
            return true;
        }
        else if (TryTriggerRelationshipQuest(ratkinOrder, map))
        {
            ratkinOrder.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.RelationshipUpgraded, cdTicks: 5 * 60000, removeWhenExpired: true);

            RelationshipKind targetRelation = ratkinOrder.Relationship.RelationshipKindOffsetBy(1);
            int recommendationNeed = RecommendationUtility.RecommendationNeed_OrderRelationUpgrade(targetRelation);
            if (recommendationNeed > 0)
            {
                RecommendationUtility.UseRecommendationOfMap(ratkinOrder, map, recommendationNeed);
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// 骑士团主动提升关系概率
    /// </summary>
    public static float GetChanceOfAutoUpgradeRelationship(this RatkinOrder ratkinOrder, bool resultOnly, out string explain)
    {
        explain = string.Empty;

        AcceptanceReport acceptanceReport = CanUpgradeRelationship(ratkinOrder, map: null, byPlayer: false, resultOnly: resultOnly);
        if (!acceptanceReport)
        {
            if (!resultOnly)
            {
                explain = (acceptanceReport.Reason + ": " + 0f.ToStringPercent()).Colorize(ColorLibrary.Grey);
            }
            return 0f;
        }

        float curChance = 1f;
        StringBuilder sb = resultOnly ? null : new StringBuilder();

        //认可度
        AddExplain((ratkinOrder.Esteem * 0.01f * 0.2f), "OARO_ChangeFactor_Esteem");

        //关系
        AddExplain(1f / ((int)ratkinOrder.Relationship + 1f), "OARO_ChangeFactor_Relationship");

        //派系关系
        if (ratkinOrder.Faction.PlayerRelationKind == FactionRelationKind.Ally)
        {
            AddExplain(1.2f, "OARO_ChangeFactor_OrderFactionAlly");
        }

        //友好分部
        int friendlyBranchCount = ratkinOrder.BranchManager.FriendlyBranchesCount.Value;
        if (friendlyBranchCount > 0)
        {
            AddExplain(1f + friendlyBranchCount * 0.1f, "OARO_ChangeFactor_FriendlyBranchesCount");
        }

        if (!resultOnly)
        {
            explain = sb.ToString();
        }
        curChance = Mathf.Clamp01(curChance);

        return curChance;

        void AddExplain(float change, string reason)
        {
            curChance *= change;
            if (!resultOnly)
            {
                sb.Append(reason.Translate(change.ToStringPercent("0.##")).Colorize(change < 1f ? ColorLibrary.Red : Color.green));
            }
        }
    }

    /// <summary>
    /// 骑士团主动提升关系
    /// </summary>
    public static void AutoUpgradeRelationship(this RatkinOrder ratkinOrder, Map map)
    {
        if (ratkinOrder.Relationship == RelationshipKind.Soulmate)
        {
            return;
        }

        ratkinOrder.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.AutoRelationshipUpgraded, cdTicks: 10 * 60000, removeWhenExpired: true);
        ChoiceLetter_AutoUpgradeRelationship letter = (ChoiceLetter_AutoUpgradeRelationship)LetterMaker.MakeLetter(
            label: "OARO_LetterLabel_AutoUpgradeRelationship",
            text: "OARO_Letter_AutoUpgradeRelationship".Translate(ratkinOrder.Name),
            def: OARO_LetterDefOf.OARO_AutoUpgradeRelationshipQuizLetter,
            relatedFaction: ratkinOrder.Faction);
        letter.RelatedOrder = ratkinOrder;
        letter.StartTimeout(30000);
        Find.LetterStack.ReceiveLetter(letter);
    }

    /// <summary>
    /// 触发关系提升任务
    /// </summary>
    public static bool TryTriggerRelationshipQuest(RatkinOrder ratkinOrder, Map map)
    {
        Slate slate = new();
        slate.SetBasicOrderSlateVar(ratkinOrder);
        slate.Set(KeyLibrary_SlateStoreAs.orderRelationship, ratkinOrder.Relationship);
        slate.Set("map", map);

        return OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, OARO_QuestScriptDefOf.OARO_Quest_OrderRelationshipUpgrade, slate, forced: true);
    }

    /// <summary>
    /// 骑士团关系变化邮件
    /// </summary>
    public static void SendNewRelationshipLetter(RatkinOrder ratkinOrder, RelationshipKind oldRelation, RelationshipKind newRelation)
    {
        bool upgraded = oldRelation < newRelation;
        StringBuilder sb = new();

        if (upgraded)
        {
            sb.AppendLine("OARO_UpgradedToNewRelation".Translate(
                ratkinOrder.NameColored.Named(KeyLibrary_FormatArgName.OrderName),
                oldRelation.GetLabel().Named("OldRelation"),
                newRelation.GetLabel().Named("NewRelation")));
        }
        else
        {
            sb.AppendLine("OARO_DowngradedToNewRelation".Translate(
                ratkinOrder.NameColored.Named(KeyLibrary_FormatArgName.OrderName),
                oldRelation.GetLabel().Named("OldRelation"),
                newRelation.GetLabel().Named("NewRelation")));
        }

        sb.AppendLine();
        if (!string.IsNullOrEmpty(ratkinOrder.EsteemHandler.LastRelationshipChangeReason))
        {
            sb.AppendLine("OARO_LastRelationshipChangeReason".Translate(ratkinOrder.EsteemHandler.LastRelationshipChangeReason.Named(KeyLibrary_FormatArgName.Reason)));
            sb.AppendLine();
        }

        sb.AppendLine(GetDescription(ratkinOrder, newRelation));
        sb.AppendLine();

        int oldIndex = (int)oldRelation;
        int newIndex = (int)newRelation;
        if (upgraded)
        {
            sb.AppendLine("OARO_Relationship_GainPermission".Translate(ratkinOrder.NameColored.Named(KeyLibrary_FormatArgName.OrderName)));
            for (int i = oldIndex + 1; i <= newIndex; i++)
            {
                sb.AppendLine($"OARO_Relationship_Permission_{(RelationshipKind)i}".Translate());
            }
        }
        else
        {
            sb.AppendLine("OARO_Relationship_LossPermission".Translate(ratkinOrder.NameColored.Named(KeyLibrary_FormatArgName.OrderName)));
            for (int i = oldIndex; i > newIndex; i--)
            {
                sb.AppendLine($"OARO_Relationship_Permission_{(RelationshipKind)i}".Translate());
            }
        }

        OrderLetterUtility.ReceiveLetter(
            label: upgraded ? "OARO_Order_Relationship_UpgradedLabel".Translate(ratkinOrder.Name.Named(KeyLibrary_FormatArgName.OrderName))
                            : "OARO_Order_Relationship_DowngradLabel".Translate(ratkinOrder.Name.Named(KeyLibrary_FormatArgName.OrderName)),
            text: sb.ToTaggedString(),
            def: OrderLetterDefOf.OARO_OfficialLetter,
            relatedOrder: ratkinOrder,
            relatedLetterType: upgraded ? OrderLetter.RelatedLetterType.Positive : OrderLetter.RelatedLetterType.Negative);
    }
}