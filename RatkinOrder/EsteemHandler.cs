using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class EsteemHandler(RatkinOrder ratkinOrder) : IExposable, ITickDay, IDrawDevWindow
{
    public enum RelationshipKind : byte
    {
        Stranger,
        Acquaintance,
        Friendly,
        Trustworthy,
        Soulmate
    }

    [Unsaved] public readonly RatkinOrder RatkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));

    /*
    认可度
    */
    protected int esteem;
    public int Esteem => esteem;
    private int lastEsteemChange;
    private string lastEsteemChangeReason = string.Empty;

    public int LastEsteemChange => lastEsteemChange;
    public string LastEsteemChangeReason => lastEsteemChangeReason;
    public float CurEsteemSoftCap => EsteemUtility.GetEsteemSoftCap(relationship);
    /*
    关系
    */
    private RelationshipKind relationship = RelationshipKind.Stranger; //当前关系
    private int lastRelationshipChangeTick = -1;
    private string lastRelationshipChangeReason = string.Empty;

    public RelationshipKind Relationship => relationship;
    public int LastRelationshipChangeTick => lastRelationshipChangeTick;
    public string LastRelationshipChangeReason => lastRelationshipChangeReason;


    private int totalRecommendation;
    public int TotalRecommendation
    {
        get => totalRecommendation;
        set => totalRecommendation += Mathf.Max(0, value);
    }

    public void PostOrderGenerated()
    {
        relationship = GameComponent_RatkinOrder.Instance?.InitOrderRelationship ?? RelationshipKind.Stranger;
        esteem = EsteemUtility.GetEsteemSoftCap(relationship);
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref esteem, "esteem", 0);

        Scribe_Values.Look(ref lastEsteemChange, "lastEsteemChange", 0);
        Scribe_Values.Look(ref lastEsteemChangeReason, "lastEsteemChangeReason", string.Empty);

        Scribe_Values.Look(ref relationship, "relationship", RelationshipKind.Stranger);
        Scribe_Values.Look(ref lastRelationshipChangeTick, "lastRelationshipChangeTick", -1);
        Scribe_Values.Look(ref lastRelationshipChangeReason, "lastRelationshipChangeReason", string.Empty);

        Scribe_Values.Look(ref totalRecommendation, "totalRecommendation", 0);
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"Esteem: {esteem}");
        listing_Rect.Label($"LastEsteemChange(by player): {lastEsteemChange}");
        listing_Rect.Label($"lastEsteemChangeReason(by player): {lastEsteemChangeReason}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"Relationship: {relationship}");
        listing_Rect.Label($"LastRelationshipChangeTick: {lastRelationshipChangeTick}");
        listing_Rect.Label($"LastRelationshipChangeReason: {lastRelationshipChangeReason}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"TotalRecommendation: {totalRecommendation}");
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
                        Messages.Message("OARO_Message_EsteemIncreaseNoReason".Translate(RatkinOrder.Name, lastEsteemChange), MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        Messages.Message("OARO_Message_EsteemIncrease".Translate(RatkinOrder.Name, lastEsteemChange, reason), MessageTypeDefOf.PositiveEvent);
                    }

                }
                else
                {
                    if (reason is null)
                    {
                        Messages.Message("OARO_Message_EsteemDecreaseNoReason".Translate(RatkinOrder.Name, lastEsteemChange), MessageTypeDefOf.NegativeEvent);
                    }
                    else
                    {
                        Messages.Message("OARO_Message_EsteemDecrease".Translate(RatkinOrder.Name, lastEsteemChange, reason), MessageTypeDefOf.NegativeEvent);
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