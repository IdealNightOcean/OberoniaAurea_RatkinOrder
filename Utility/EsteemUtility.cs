using RimWorld;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class EsteemUtility
{

    /// <summary>
    /// 同时改变所有骑士团认可度
    /// </summary>
    public static void AdjustAllOrdersEsteem(int change, bool byPlayer, bool showPlayerChangeMessage = true, string reason = null)
    {
        foreach (RatkinOrder order in RatkinOrderManager.Instance.AllRatkinOrders)
        {
            order.EsteemHandler.AdjustEsteem(change, byPlayer, showPlayerChangeMessage: false, reason);
        }

        if (showPlayerChangeMessage)
        {
            if (change > 0)
            {
                if (reason is null)
                {
                    Messages.Message("OARO_Message_AllOrdersEsteemIncreaseNoReason".Translate(change), MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    Messages.Message("OARO_Message_AllOrdersEsteemIncrease".Translate(change, reason), MessageTypeDefOf.PositiveEvent);
                }

            }
            else
            {
                if (reason is null)
                {
                    Messages.Message("OARO_Message_AllOrdersEsteemDecreaseNoReason".Translate(change), MessageTypeDefOf.NegativeEvent);
                }
                else
                {
                    Messages.Message("OARO_Message_AllOrdersEsteemDecrease".Translate(change, reason), MessageTypeDefOf.NegativeEvent);
                }
            }
        }
    }

    /// <summary>
    /// 关系类型枚举数组
    /// </summary>
    public static readonly OrderRelationshipKind[] RelationshipKindArr = (OrderRelationshipKind[])Enum.GetValues(typeof(OrderRelationshipKind));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OrderRelationshipKind RelationshipKindOffsetBy(this OrderRelationshipKind relationship, int offset)
    {
        return RelationshipKindArr[Mathf.Clamp((int)relationship + offset, 0, RelationshipKindArr.Length - 1)];
    }

    public static void RelationshipKindOffsetBy(this RatkinOrder order, int offset)
    {
        if (offset == 0)
        {
            return;
        }
        order.EsteemHandler.SetRelationship(order.Relationship.RelationshipKindOffsetBy(offset));
    }

    /// <summary>
    /// 认可度软上限数组
    /// </summary>
    private static readonly int[] EsteemSoftCap = [30, 50, 70, 90, 100];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetEsteemSoftCap(OrderRelationshipKind relationship)
    {
        return EsteemSoftCap[Mathf.Clamp((int)relationship, 0, EsteemSoftCap.Length - 1)];
    }

    /// <summary>
    /// 认可度称号
    /// </summary>
    private static readonly (string, Color)[] EsteemTitles =
    [
        ("OARO_EsteemStranger", Color.white),
        ("OARO_EsteemAcquaintance", Color.cyan),
        ("OARO_EsteemNewFriend", Color.green),
        ("OARO_EsteemFriend", Color.green),
        ("OARO_EsteemGoodFriend", Color.yellow),
        ("OARO_EsteemTrustworthy", Color.yellow),
        ("OARO_EsteemSoulmate", ColorLibrary.Gold),
    ];

    /// <summary>
    /// 认可度描述
    /// </summary>
    private static readonly (string, Color)[] EsteemDescs =
    [
        ("OARO_EsteemStrangerDesc", Color.white),
        ("OARO_EsteemAcquaintanceDesc", Color.cyan),
        ("OARO_EsteemNewFriendDesc", Color.green),
        ("OARO_EsteemFriendDesc", Color.green),
        ("OARO_EsteemGoodFriendDesc", Color.yellow),
        ("OARO_EsteemTrustworthyDesc", Color.yellow),
        ("OARO_EsteemSoulmateDesc", ColorLibrary.Gold),
    ];
    public static string GetEsteemTitle(int esteem)
    {
        int index = esteem switch
        {
            < 10 => 0,
            < 30 => 1,
            < 50 => 2,
            < 70 => 3,
            < 90 => 4,
            < 99 => 5,
            _ => 6
        };

        (string, Color) esteemTitle = EsteemTitles[index];
        return esteemTitle.Item1.Translate().Colorize(esteemTitle.Item2);
    }
    public static string GetEsteemDesc(this RatkinOrder order, int esteem)
    {
        int index = esteem switch
        {
            < 10 => 0,
            < 30 => 1,
            < 50 => 2,
            < 70 => 3,
            < 90 => 4,
            < 99 => 5,
            _ => 6
        };

        (string, Color) esteemDesc = EsteemDescs[index];
        return esteemDesc.Item1.Translate(order.Name).Colorize(esteemDesc.Item2);
    }

    /// <summary>
    /// 关系称号
    /// </summary>
    private static readonly (string, Color)[] RelationshipKindLabels =
    [
        ("OARO_RelationStranger", Color.white),
        ("OARO_RelationAcquaintance", Color.cyan),
        ("OARO_RelationFriendly", Color.green),
        ("OARO_RelationTrustworthy", Color.green),
        ("OARO_RelationSoulmate", Color.green),
    ];
    /// <summary>
    /// 关系描述
    /// </summary>
    private static readonly (string, Color)[] RelationshipKindDescs =
    [
        ("OARO_RelationStrangerDesc", Color.white),
        ("OARO_RelationAcquaintanceDesc", Color.cyan),
        ("OARO_RelationFriendlyDesc", Color.green),
        ("OARO_RelationTrustworthyDesc", Color.green),
        ("OARO_RelationSoulmateDesc", Color.green),
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color GetRelationshipColor(this OrderRelationshipKind relationship)
    {
        return relationship switch
        {
            OrderRelationshipKind.Stranger => Color.white,
            OrderRelationshipKind.Acquaintance => Color.cyan,
            OrderRelationshipKind.Friendly => Color.green,
            OrderRelationshipKind.Trustworthy => Color.green,
            OrderRelationshipKind.Soulmate => Color.green,
            _ => Color.white
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetRelationshipKindLabel(this OrderRelationshipKind relationship)
    {
        int index = Mathf.Clamp((int)relationship, 0, RelationshipKindLabels.Length - 1);
        (string, Color) relationshipLabel = RelationshipKindLabels[index];
        return relationshipLabel.Item1.Translate().Colorize(relationshipLabel.Item2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetRelationshipKindDesc(RatkinOrder order, OrderRelationshipKind relationship)
    {
        int index = Mathf.Clamp((int)relationship, 0, RelationshipKindDescs.Length - 1);
        (string, Color) relationshipLabel = RelationshipKindDescs[index];
        return relationshipLabel.Item1.Translate(order.Name).Colorize(relationshipLabel.Item2);
    }

    /// <summary>
    /// 能否提升关系类型等级
    /// </summary>
    public static AcceptanceReport CanUpgradeRelationship(this RatkinOrder order, bool byPlayer, bool resultOnly)
    {
        OrderRelationshipKind curRelationship = order.Relationship;
        if (curRelationship == OrderRelationshipKind.Soulmate)
        {
            return resultOnly ? false : "OARO_Max_OrderRelationship".Translate();
        }

        if (byPlayer)
        {
            if (order.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.RelationshipUpgraded))
            {
                return resultOnly ? false : "OARO_Cooling_RelationshipUpgraded".Translate();
            }
        }
        else
        {
            if (order.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.AutoRelationshipUpgraded))
            {
                return resultOnly ? false : "OARO_Cooling_AutoRelationshipUpgraded".Translate();
            }
            if (order.Faction.HostileTo(Faction.OfPlayer))
            {
                return resultOnly ? false : "OARO_OrderFaction_Hostile".Translate();
            }
        }

        OrderRelationshipKind newRelationship = curRelationship.RelationshipKindOffsetBy(1);

        switch (newRelationship)
        {
            case OrderRelationshipKind.Acquaintance:
                return ValidateRelationshipRequirement(esteem: 20, totalRecommendation: 1);
            case OrderRelationshipKind.Friendly:
                return ValidateRelationshipRequirement(esteem: 30, totalRecommendation: 3);
            case OrderRelationshipKind.Trustworthy:
                if (order.BranchManager.NormalDemandFulfillCount < 2)
                {
                    return resultOnly ? false : "OARO_Insufficient_NormalDemandFulfill".Translate(2);
                }
                return ValidateRelationshipRequirement(esteem: 40, totalRecommendation: 6, friendlyBranchesCount: 1);
            case OrderRelationshipKind.Soulmate:
                if (order.BranchManager.CriticalDemandFulfillCount < 2)
                {
                    return resultOnly ? false : "OARO_Insufficient_CriticalDemandFulfill".Translate(2);
                }
                return ValidateRelationshipRequirement(esteem: 50, totalRecommendation: 12, friendlyBranchesCount: 3);
            default:
                return true;
        }

        AcceptanceReport ValidateRelationshipRequirement(int esteem, int totalRecommendation, int friendlyBranchesCount = -1)
        {
            if (!byPlayer && order.Esteem < esteem)
            {
                return resultOnly ? false : "OARO_Insufficient_Esteem".Translate(esteem);
            }
            if (order.EsteemHandler.TotalRecommendation < totalRecommendation)
            {
                return resultOnly ? false : "OARO_Insufficient_TotalRecommendation".Translate(totalRecommendation);
            }
            if (friendlyBranchesCount > 0 && order.BranchManager.FriendlyBranchesCount < friendlyBranchesCount)
            {
                return resultOnly ? false : "OARO_Insufficient_FriendlyBranches".Translate(friendlyBranchesCount);
            }
            return true;
        }
    }

    /// <summary>
    /// 提升骑士团关系（玩家）
    /// </summary>
    public static void UpgradeRelationship(RatkinOrder order)
    {
        if (order.Relationship == OrderRelationshipKind.Soulmate)
        {
            return;
        }

        order.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.RelationshipUpgraded, cdTicks: 5 * 60000, shouldRemoveWhenExpired: true);
        throw new NotImplementedException();
    }

    /// <summary>
    /// 骑士团主动提升关系概率
    /// </summary>
    public static float GetChanceOfAutoUpgradeRelationship(this RatkinOrder order, bool resultOnly, out string explain)
    {
        explain = string.Empty;

        AcceptanceReport acceptanceReport = CanUpgradeRelationship(order, byPlayer: false, resultOnly: resultOnly);
        if (!acceptanceReport)
        {
            if (!resultOnly)
            {
                explain = (acceptanceReport.Reason + ": " + 0f.ToStringPercent()).Colorize(ColorLibrary.Grey);
            }
            return 0f;
        }

        float chance = 1f;
        float curStepChange;
        StringBuilder sb = resultOnly ? null : new StringBuilder();

        //认可度
        curStepChange = (order.Esteem * 0.01f * 0.2f);
        chance *= curStepChange;
        if (!resultOnly)
        {
            sb.Append("OARO_AutoUpgradeRelation_Esteem".Translate(curStepChange.ToStringPercentSigned()));
        }

        //关系
        curStepChange = 1f / ((int)order.Relationship + 1f);
        chance *= curStepChange;
        if (!resultOnly)
        {
            sb.Append("OARO_AutoUpgradeRelation_Relationship".Translate(GetRelationshipKindLabel(order.Relationship), curStepChange.ToStringPercentSigned()));
        }

        //派系关系
        if (order.Faction.PlayerRelationKind == FactionRelationKind.Ally)
        {
            curStepChange = 1.2f;
            chance *= curStepChange;
            if (!resultOnly)
            {
                sb.Append("OARO_AutoUpgradeRelation_FactionAlly".Translate(curStepChange.ToStringPercentSigned()).Colorize(Color.green));
            }
        }

        //友好分部
        int friendlyBranchCount = order.BranchManager.FriendlyBranchesCount;
        if (friendlyBranchCount > 0)
        {
            curStepChange = 1f + friendlyBranchCount * 0.1f;
            chance *= curStepChange;
            if (!resultOnly)
            {
                sb.Append("OARO_AutoUpgradeRelation_FriendlyBranch".Translate(friendlyBranchCount, curStepChange.ToStringPercentSigned()).Colorize(Color.green));
            }
        }

        if (!resultOnly)
        {
            explain = sb.ToString();
        }
        chance = Mathf.Clamp01(chance);

        return chance;
    }

    /// <summary>
    /// 骑士团主动提升关系
    /// </summary>
    public static void AutoUpgradeRelationship(this RatkinOrder order)
    {
        if (order.Relationship == OrderRelationshipKind.Soulmate)
        {
            return;
        }

        order.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.AutoRelationshipUpgraded, cdTicks: 10 * 60000, shouldRemoveWhenExpired: true);
        throw new NotImplementedException();
    }
}