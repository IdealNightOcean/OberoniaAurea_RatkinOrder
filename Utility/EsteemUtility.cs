using OberoniaAurea_Frame;
using RimWorld;
using System.Runtime.CompilerServices;
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
                    Messages.Message("OARO_Message_AllOrdersEsteemIncreaseNoReason".Translate(change.Named(KeyLibrary_FormatArgName.Count)), MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    Messages.Message(
                        "OARO_Message_AllOrdersEsteemIncrease".Translate(change.Named(KeyLibrary_FormatArgName.Count), reason.Named(KeyLibrary_FormatArgName.Reason)),
                        MessageTypeDefOf.PositiveEvent);
                }
            }
            else
            {
                if (reason is null)
                {
                    Messages.Message("OARO_Message_AllOrdersEsteemDecreaseNoReason".Translate(change.Named(KeyLibrary_FormatArgName.Count)), MessageTypeDefOf.NegativeEvent);
                }
                else
                {
                    Messages.Message(
                        "OARO_Message_AllOrdersEsteemDecrease".Translate(change.Named(KeyLibrary_FormatArgName.Count), reason.Named(KeyLibrary_FormatArgName.Reason)),
                        MessageTypeDefOf.NegativeEvent);
                }
            }
        }
    }

    /// <summary>
    /// 认可度软上限数组
    /// </summary>
    private static readonly int[] EsteemSoftCap = [30, 50, 70, 90, 100];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetEsteemSoftCap(EsteemHandler.RelationshipKind relationship)
    {
        return EsteemSoftCap[Mathf.Clamp((int)relationship, 0, EsteemSoftCap.Length - 1)];
    }

    /// <summary>
    /// 认可度称号
    /// </summary>
    private static readonly (string, Color)[] EsteemTitles =
    [
        ("OARO_Esteem_Stranger", Color.white),
        ("OARO_Esteem_Acquaintance", Color.cyan),
        ("OARO_Esteem_NewFriend", Color.green),
        ("OARO_Esteem_Friend", Color.green),
        ("OARO_Esteem_GoodFriend", Color.yellow),
        ("OARO_Esteem_Trustworthy", Color.yellow),
        ("OARO_Esteem_Soulmate", ColorLibrary.Gold),
    ];

    /// <summary>
    /// 认可度描述
    /// </summary>
    private static readonly (string, Color)[] EsteemDescs =
    [
        ("OARO_EsteemDesc_Stranger", Color.white),
        ("OARO_EsteemDesc_Acquaintance", Color.cyan),
        ("OARO_EsteemDesc_NewFriend", Color.green),
        ("OARO_EsteemDesc_Friend", Color.green),
        ("OARO_EsteemDesc_GoodFriend", Color.yellow),
        ("OARO_EsteemDesc_Trustworthy", Color.yellow),
        ("OARO_EsteemDesc_Soulmate", ColorLibrary.Gold),
    ];

    public static string GetEsteemTitle(int esteem)
    {
        (string, Color) esteemTitle = EsteemTitles[GetIndex(esteem)];
        return esteemTitle.Item1.Translate().Colorize(esteemTitle.Item2);
    }
    public static string GetEsteemDesc(this RatkinOrder ratkinOrder, int esteem)
    {
        (string, Color) esteemDesc = EsteemDescs[GetIndex(esteem)];
        return esteemDesc.Item1.Translate(ratkinOrder.Name).Colorize(esteemDesc.Item2);
    }

    public static int GetIndex(int esteem)
    {
        return esteem switch
        {
            < 10 => 0,
            < 30 => 1,
            < 50 => 2,
            < 70 => 3,
            < 90 => 4,
            < 99 => 5,
            _ => 6
        };
    }
}