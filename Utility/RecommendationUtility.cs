using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

using static OberoniaAurea.RatkinOrder.EsteemHandler;

public static class RecommendationUtility
{
    public static OrderRecommendation MakeRecommendationForPlayer(int count)
    {
        OrderRecommendation recommendation = (OrderRecommendation)ThingMaker.MakeThing(OARO_ThingDefOf.OARO_OrderRecommendation);
        recommendation.stackCount = count;
        recommendation.OnMakeForPlayer();
        return recommendation;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CurRecommendationCount(this Map map)
    {
        return map?.listerThings.ThingsOfDef(OARO_ThingDefOf.OARO_OrderRecommendation)?.Sum(t => t.stackCount) ?? 0;
    }

    public static bool HasEnoughRecommendation(this Map map, int count)
    {
        return map.HasEnoughThingsOfDef(OARO_ThingDefOf.OARO_OrderRecommendation, count);
    }

    public static bool HasEnoughRecommendation(this Caravan caravan, int count)
    {
        if (caravan is null) return false;
        return CaravanInventoryUtility.HasThings(caravan, OARO_ThingDefOf.OARO_OrderRecommendation, count);
    }

    public static void GiveRecommendationsToPlayer(int count, Action<Thing> giveAction)
    {
        if (count <= 0 || giveAction is null)
        {
            return;
        }

        OrderRecommendation recommendations = MakeRecommendationForPlayer(count);
        giveAction.Invoke(recommendations);
    }

    public static void GiveRecommendationsToCaravan(Caravan caravan, int count, bool sendStandLetter = true, RatkinOrder ratkinOrder = null)
    {
        if (caravan is null || count <= 0)
        {
            return;
        }
        OrderRecommendation recommendations = MakeRecommendationForPlayer(count);
        CaravanInventoryUtility.GiveThing(caravan, recommendations);
        if (sendStandLetter)
        {
            SendStandardGetRecommendationLetter(count, ratkinOrder, caravan);
        }
    }

    public static void GiveRecommendationsToFixedCaravan(FixedCaravan fixedCaravan, int count, bool sendStandLetter = true, RatkinOrder ratkinOrder = null)
    {
        if (fixedCaravan is null || count <= 0)
        {
            return;
        }
        OrderRecommendation recommendations = MakeRecommendationForPlayer(count);
        OAFrame_FixedCaravanUtility.GiveThing(fixedCaravan, recommendations);
        if (sendStandLetter)
        {
            SendStandardGetRecommendationLetter(count, ratkinOrder, fixedCaravan);
        }
    }

    public static void GiveRecommendationsToPlayerMap(Map map, int count, bool sendStandLetter = true, RatkinOrder ratkinOrder = null, IntVec3? spawnCell = null, bool dropPod = false)
    {
        if (map is null || count <= 0)
        {
            return;
        }
        OrderRecommendation recommendations = MakeRecommendationForPlayer(count);

        Faction faction = ratkinOrder?.Faction;

        if (dropPod)
        {
            spawnCell ??= DropCellFinder.TradeDropSpot(map);
            DropPodUtility.DropThingsNear(spawnCell.Value, map, [recommendations], forbid: false, allowFogged: false, faction: faction);
        }
        else
        {
            if (!spawnCell.HasValue)
            {
                CellFinder.TryRandomClosewalkCellNear(map.Center, map, 100, out IntVec3 cell);
                spawnCell = cell;
            }
            GenPlace.TryPlaceThing(recommendations, spawnCell.Value, map, ThingPlaceMode.Near);
        }

        if (sendStandLetter)
        {
            SendStandardGetRecommendationLetter(count, ratkinOrder, new LookTargets(spawnCell.Value, map));
        }
    }

    /// <returns>实际使用数</returns>
    public static int UseRecommendationOfMap(Map map, int useCount)
    {
        if (useCount <= 0 || map is null)
        {
            return Mathf.Max(useCount, 0);
        }

        List<Thing> recommendations = OAFrame_MapUtility.TakeThingsOfDef(
            map: map,
            thingDef: OARO_ThingDefOf.OARO_OrderRecommendation,
            count: useCount,
            actualTakeCount: out int actualTakeCount);

        if (recommendations.NullOrEmpty())
        {
            return useCount;
        }

        for (int i = recommendations.Count - 1; i >= 0; i--)
        {
            recommendations[i].Destroy();
        }

        return actualTakeCount;
    }

    /// <returns>实际使用数</returns>
    public static int UseRecommendationOfCaravan(Caravan caravan, int useCount)
    {
        if (useCount <= 0 || caravan is null)
        {
            return Mathf.Max(useCount, 0);
        }

        return OAFrame_CaravanUtility.RemoveThingsOfDef(caravan: caravan,
                                                        thingDef: OARO_ThingDefOf.OARO_OrderRecommendation,
                                                        count: useCount);

    }

    /// <returns>实际使用数</returns>
    public static int UseRecommendationOfFixedCaravan(FixedCaravan fixedCaravan, int useCount)
    {
        if (useCount <= 0 || fixedCaravan is null)
        {
            return Mathf.Max(useCount, 0);
        }

        return OAFrame_FixedCaravanUtility.RemoveThingsOfDef(fixedCaravan: fixedCaravan,
                                                            thingDef: OARO_ThingDefOf.OARO_OrderRecommendation,
                                                            count: useCount);
    }



    /// <summary>
    /// 提升到新等级需要的推荐信数量
    /// </summary>
    /// <param name="targetRelation">目标关系等级</param>
    public static int RecommendationNeed_OrderRelationUpgrade(RelationshipKind targetRelation)
    {
        return targetRelation switch
        {
            RelationshipKind.Trustworthy => 1,
            RelationshipKind.Soulmate => 2,
            _ => 0
        };
    }

    /// <summary>
    /// 招募骑士所需推荐信数量
    /// </summary>
    public static int RecommendationNeed_RecruitmentKnight(RatkinOrder ratkinOrder)
    {
        return ratkinOrder.Esteem switch
        {
            < 50 => 3,
            < 90 => 2,
            _ => 1
        };
    }

    /// <summary>
    /// 提升常驻骑士阶位所需推荐信数量
    /// </summary>
    /// <param name="targetRank">目标阶位</param>
    public static int RecommendationNeed_ResidentKnightRankUpgrade(RatkinOrder ratkinOrder, ResidentKnightRecord.Rank targetRank)
    {
        int needCount = targetRank switch
        {
            ResidentKnightRecord.Rank.Regular => 0,
            ResidentKnightRecord.Rank.Elite => 1,
            ResidentKnightRecord.Rank.Honor => 2,
            ResidentKnightRecord.Rank.Crown => 4,
            _ => 0
        };
        if (ratkinOrder.Relationship >= EsteemHandler.RelationshipKind.Soulmate)
        {
            needCount--;
        }
        return needCount > 0 ? needCount : 0;
    }

    private static void SendStandardGetRecommendationLetter(int count, RatkinOrder ratkinOrder = null, LookTargets lookTargets = null)
    {
        ChoiceLetter_RatkinOrder letter = (ChoiceLetter_RatkinOrder)LetterMaker.MakeLetter(
            label: "OARO_LetterLabel_GetRecommendation".Translate(),
            text: ratkinOrder is null ? "OARO_Letter_GetRecommendation".Translate(count.Named(KeyLibrary_FormatArgName.Count))
                                      : "OARO_Letter_GetRecommendation_HasOrder".Translate(ratkinOrder.NameColored.Named(KeyLibrary_FormatArgName.OrderName), count.Named(KeyLibrary_FormatArgName.Count)),
            def: OARO_LetterDefOf.OARO_Order_PositiveLetter,
            lookTargets: lookTargets,
            relatedFaction: ratkinOrder?.Faction);
        letter.RelatedOrder = ratkinOrder;
        Find.LetterStack.ReceiveLetter(letter);
    }
}
