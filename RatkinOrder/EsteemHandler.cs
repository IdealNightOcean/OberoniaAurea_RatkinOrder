using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 认可度 | 关系 | 推荐信
/// </summary>
public class EsteemHandler : IExposable, ITickDay
{
    public enum RelationshipKind : byte
    {
        /// <summary>
        /// 陌生
        /// </summary>
        Stranger,
        /// <summary>
        /// 熟识
        /// </summary>
        Acquaintance,
        /// <summary>
        /// 友好
        /// </summary>
        Friendly,
        /// <summary>
        /// 信赖
        /// </summary>
        Trustworthy,
        /// <summary>
        /// 同途
        /// </summary>
        Soulmate
    }

    public RatkinOrder RatkinOrder { get; }

    protected int esteem;
    public int Esteem => esteem;
    private int lastEsteemChange;
    private string lastEsteemChangeReason = string.Empty;

    public int LastEsteemChange => lastEsteemChange;
    public string LastEsteemChangeReason => lastEsteemChangeReason;
    public float CurEsteemSoftCap => EsteemUtility.GetEsteemSoftCap(relationship);


    private RelationshipKind relationship = RelationshipKind.Stranger; //当前关系
    private int lastRelationshipChangeTick = -1;
    private string lastRelationshipChangeReason = string.Empty;

    public RelationshipKind Relationship => relationship;
    public int LastRelationshipChangeTick => lastRelationshipChangeTick;
    public string LastRelationshipChangeReason => lastRelationshipChangeReason;


    public EsteemHandler(RatkinOrder ratkinOrder)
    {
        RatkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
    }

    public void PostOrderGenerated()
    {
        relationship = GameComponent_RatkinOrder.Instance?.InitOrderRelationship ?? RelationshipKind.Stranger;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref esteem, nameof(esteem), 0);

        Scribe_Values.Look(ref lastEsteemChange, nameof(lastEsteemChange), 0);
        Scribe_Values.Look(ref lastEsteemChangeReason, nameof(lastEsteemChangeReason), string.Empty);

        Scribe_Values.Look(ref relationship, nameof(relationship), RelationshipKind.Stranger);
        Scribe_Values.Look(ref lastRelationshipChangeTick, nameof(lastRelationshipChangeTick), -1);
        Scribe_Values.Look(ref lastRelationshipChangeReason, nameof(lastRelationshipChangeReason), string.Empty);
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"认可度: {esteem}");
        listing_Rect.Label($"最近变化 (by player): {lastEsteemChange}");
        listing_Rect.Label($"最近变化的原因 (by player): {lastEsteemChangeReason}");
        if (listing_Rect.ButtonText("+10 认可度", widthPct: 0.5f))
        {
            AdjustEsteem(10, byPlayer: true, showPlayerChangeMessage: false, reason: "Dev");
        }
        if (listing_Rect.ButtonText("-10 认可度", widthPct: 0.5f))
        {
            AdjustEsteem(10, byPlayer: true, showPlayerChangeMessage: false, reason: "Dev");
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label($"关系: {relationship}");
        listing_Rect.Label($"最近变化时刻 (Tick): {lastRelationshipChangeTick}");
        listing_Rect.Label($"最近变化原因: {lastRelationshipChangeReason}");
        if (listing_Rect.ButtonText("提升一级关系", widthPct: 0.5f))
        {
            SetRelationship(relationship.RelationshipKindOffsetBy(1), reason: "Dev", sendLetter: false);
        }
        if (listing_Rect.ButtonText("降低一级关系", widthPct: 0.5f))
        {
            SetRelationship(relationship.RelationshipKindOffsetBy(-1), reason: "Dev", sendLetter: false);
        }

    }

    public void TickDay()
    {
        if (esteem > CurEsteemSoftCap)
        {
            esteem--;
        }

        if (Rand.Value < RatkinOrder.GetChanceOfAutoUpgradeRelationship(resultOnly: true, out _))
        {
            Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
            if (map is not null)
            {
                RatkinOrder.AutoUpgradeRelationship(map);
            }
        }
    }

    public void Notify_FactionRelationChanged(FactionRelationKind newRelation)
    {
        if (newRelation == FactionRelationKind.Hostile)
        {
            SetRelationship(relationship.RelationshipKindOffsetBy(-1), reason: "OARO_OrderFaction_Hostile".Translate(), sendLetter: true);
        }
    }

    public void AdjustEsteem(int change, bool byPlayer = false, bool showPlayerChangeMessage = true, string reason = null)
    {
        int trueChange = esteem;
        esteem = Mathf.Clamp(esteem + change, 0, 100);

        trueChange = esteem - trueChange;
        if (trueChange != 0 && byPlayer)
        {
            lastEsteemChange = trueChange;
            lastEsteemChangeReason = reason ?? string.Empty;
            if (showPlayerChangeMessage)
            {
                if (trueChange > 0)
                {
                    if (reason is null)
                    {
                        Messages.Message(
                            text: "OARO_Message_EsteemIncreaseNoReason".Translate(RatkinOrder.Name.Named(KeyLibrary_FormatArgName.OrderName),
                                                                                  lastEsteemChange.Named(KeyLibrary_FormatArgName.Count)),
                            def: MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        Messages.Message(
                            text: "OARO_Message_EsteemIncrease".Translate(RatkinOrder.Name.Named(KeyLibrary_FormatArgName.OrderName),
                                                                          lastEsteemChange.Named(KeyLibrary_FormatArgName.Count),
                                                                          reason.Named(KeyLibrary_FormatArgName.Reason)),
                            def: MessageTypeDefOf.PositiveEvent);
                    }

                }
                else
                {
                    if (reason is null)
                    {
                        Messages.Message(
                            text: "OARO_Message_EsteemDecreaseNoReason".Translate(RatkinOrder.Name.Named(KeyLibrary_FormatArgName.OrderName),
                                                                                  lastEsteemChange.Named(KeyLibrary_FormatArgName.Count)),
                            def: MessageTypeDefOf.NegativeEvent);
                    }
                    else
                    {
                        Messages.Message(
                            text: "OARO_Message_EsteemDecrease".Translate(RatkinOrder.Name.Named(KeyLibrary_FormatArgName.OrderName),
                                                                          lastEsteemChange.Named(KeyLibrary_FormatArgName.Count),
                                                                          reason.Named(KeyLibrary_FormatArgName.Reason)),
                            def: MessageTypeDefOf.NegativeEvent);
                    }
                }
            }
        }
    }

    public void SetRelationship(RelationshipKind newRelationship, string reason, bool sendLetter)
    {
        if (newRelationship == relationship)
        {
            return;
        }
        RelationshipKind oldRelationship = relationship;

        relationship = newRelationship;

        lastRelationshipChangeReason = reason ?? string.Empty;
        lastRelationshipChangeTick = Find.TickManager.TicksGame;

        if (sendLetter)
        {
            RelationshipUtility.SendNewRelationshipLetter(RatkinOrder, oldRelationship, newRelationship);
        }
    }
}