using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
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
    private int lastRelationChangeTick = -1;
    private string lastRelationshipChangeReason = string.Empty;

    public RelationshipKind Relationship => relationship;
    public int LastRelationChangeTick => lastRelationChangeTick;
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
        Scribe_Values.Look(ref lastRelationChangeTick, nameof(lastRelationChangeTick), -1);
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
        listing_Rect.Label($"最近变化时刻 (Tick): {lastRelationChangeTick}");
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

        float autoUpgradeChance = RatkinOrder.GetChanceOfAutoUpgradeRelationship(resultOnly: true, out _);
        if (Rand.Chance(autoUpgradeChance))
        {
            Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
            if (map is not null)
            {
                AutoUpgradeRelationship();
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
                            text: "OARO_Message_EsteemIncreaseNoReason".Translate(RatkinOrder.Name.Named(OARO_KeyLibrary_FormatArgName.OrderName),
                                                                                  lastEsteemChange.Named(KeyLibrary_FormatArgName.Count)),
                            def: MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        Messages.Message(
                            text: "OARO_Message_EsteemIncrease".Translate(RatkinOrder.Name.Named(OARO_KeyLibrary_FormatArgName.OrderName),
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
                            text: "OARO_Message_EsteemDecreaseNoReason".Translate(RatkinOrder.Name.Named(OARO_KeyLibrary_FormatArgName.OrderName),
                                                                                  lastEsteemChange.Named(KeyLibrary_FormatArgName.Count)),
                            def: MessageTypeDefOf.NegativeEvent);
                    }
                    else
                    {
                        Messages.Message(
                            text: "OARO_Message_EsteemDecrease".Translate(RatkinOrder.Name.Named(OARO_KeyLibrary_FormatArgName.OrderName),
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
        lastRelationChangeTick = Find.TickManager.TicksGame;

        if (sendLetter)
        {
            RelationshipUtility.SendNewRelationshipLetter(RatkinOrder, oldRelationship, newRelationship);
        }
    }

    /// <summary>
    /// 骑士团主动提升关系
    /// </summary>
    private void AutoUpgradeRelationship()
    {
        if (relationship == RelationshipKind.Soulmate)
        {
            return;
        }

        RatkinOrder.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.AutoRelationshipUpgraded, cdTicks: 10 * 60000, removeWhenExpired: true);
        ChoiceLetter_AutoUpgradeRelationship letter = (ChoiceLetter_AutoUpgradeRelationship)LetterMaker.MakeLetter(
            label: "OARO_LetterLabel_AutoUpgradeRelationship".Translate(),
            text: "OARO_Letter_AutoUpgradeRelationship".Translate(RatkinOrder.NameColored.Named(OARO_KeyLibrary_FormatArgName.OrderName)),
            def: OARO_LetterDefOf.OARO_AutoUpgradeRelationshipQuizLetter,
            relatedFaction: RatkinOrder.Faction);
        letter.RelatedOrder = RatkinOrder;
        letter.StartTimeout(30000);
        Find.LetterStack.ReceiveLetter(letter);
    }
}