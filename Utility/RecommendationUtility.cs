using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.EsteemHandler;

namespace OberoniaAurea.RatkinOrder;

public static class RecommendationUtility
{
    public static bool IsRecommendationOfOrder(this Thing t, RatkinOrder order)
    {
        if (t is null || t.def != OARO_ThingDefOf.OARO_OrderRecommendation)
        {
            return false;
        }

        return ((OrderRecommendation)t).RatkinOrder == order;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CurRecommendationOfMap(RatkinOrder order, Map map)
    {
        return map?.listerThings.ThingsOfDef(OARO_ThingDefOf.OARO_OrderRecommendation)?.OfType<OrderRecommendation>().Where(r => r.RatkinOrder == order).Count() ?? 0;
    }

    public static void GiveRecommendationsToPlayer(RatkinOrder order, int count, Action<Thing> giveAction)
    {
        if (order is null || count <= 0 || giveAction is null)
        {
            return;
        }

        OrderRecommendation recommendations = (OrderRecommendation)ThingMaker.MakeThing(OARO_ThingDefOf.OARO_OrderRecommendation);
        recommendations.stackCount = count;
        recommendations.SetRatkinOrder(order);

        order.EsteemHandler.TotalRecommendation += count;

        recommendations.OnGiveToPlayer();
        giveAction.Invoke(recommendations);

    }

    public static void GiveRecommendationsToPlayer_Map(RatkinOrder ratkinOrder, int count, Map map, bool sendStandLetter = true, IntVec3? spawnCell = null, bool dropPod = false)
    {
        GiveRecommendationsToPlayer(ratkinOrder, count, MapGiveAction);

        void MapGiveAction(Thing recommendations)
        {
            if (dropPod)
            {
                spawnCell ??= DropCellFinder.TradeDropSpot(map);
                DropPodUtility.DropThingsNear(spawnCell.Value, map, [recommendations], allowFogged: false, faction: ratkinOrder.Faction);
                if (sendStandLetter)
                {
                    ChoiceLetter_RatkinOrder letter = (ChoiceLetter_RatkinOrder)LetterMaker.MakeLetter(
                        label: "OARO_LetterLabel_GetRecommendation_DropPod".Translate(),
                        text: "OARO_Letter_GetRecommendation_DropPod".Translate(ratkinOrder.Name, count),
                        def: OARO_LetterDefOf.OARO_Order_PositiveLetter,
                        lookTargets: new LookTargets(spawnCell.Value, map),
                        relatedFaction: ratkinOrder.Faction);
                    letter.relatedOrder = ratkinOrder;
                    Find.LetterStack.ReceiveLetter(letter);
                }
            }
            else
            {
                if (!spawnCell.HasValue)
                {
                    CellFinder.TryRandomClosewalkCellNear(map.Center, map, 100, out IntVec3 cell);
                    spawnCell = cell;
                }
                GenPlace.TryPlaceThing(recommendations, spawnCell.Value, map, ThingPlaceMode.Near);
                if (sendStandLetter)
                {
                    ChoiceLetter_RatkinOrder letter = (ChoiceLetter_RatkinOrder)LetterMaker.MakeLetter(
                        label: "OARO_LetterLabel_GetRecommendation_Map".Translate(),
                        text: "OARO_Letter_GetRecommendation_Map".Translate(ratkinOrder.Name, count),
                        def: OARO_LetterDefOf.OARO_Order_PositiveLetter,
                        lookTargets: new LookTargets(spawnCell.Value, map),
                        relatedFaction: ratkinOrder.Faction);
                    letter.relatedOrder = ratkinOrder;
                    Find.LetterStack.ReceiveLetter(letter);
                }
            }
        }
    }

    /// <returns>实际使用数</returns>
    public static int UseRecommendationOfMap(RatkinOrder order, Map map, int useCount)
    {
        if (useCount <= 0 || map is null)
        {
            return Mathf.Max(useCount, 0);
        }

        List<Thing> recommendations = OAFrame_MapUtility.TakeThingsOfDef(
            map: map,
            thingDef: OARO_ThingDefOf.OARO_OrderRecommendation,
            count: useCount,
            validator: (t) => ((OrderRecommendation)t).RatkinOrder == order,
            actualTakeCount: out int actualTakeCount);

        if (recommendations.NullOrEmpty())
        {
            return useCount;
        }

        for (int i = recommendations.Count; i >= 0; i--)
        {
            recommendations[i].Destroy();
        }

        return actualTakeCount;
    }

    /// <returns>实际使用数</returns>
    public static int UseRecommendationOfCaravan(RatkinOrder order, Caravan caravan, int useCount)
    {
        if (useCount <= 0 || caravan is null)
        {
            return Mathf.Max(useCount, 0);
        }

        return OAFrame_CaravanUtility.RemoveThingsOfDef(caravan: caravan,
                                                        thingDef: OARO_ThingDefOf.OARO_OrderRecommendation,
                                                        count: useCount,
                                                        validator: (t) => ((OrderRecommendation)t).RatkinOrder == order);

    }

    /// <returns>实际使用数</returns>
    public static int UseRecommendationOfFixedCaravan(RatkinOrder order, FixedCaravan fixedCaravan, int useCount)
    {
        if (useCount <= 0 || fixedCaravan is null)
        {
            return Mathf.Max(useCount, 0);
        }

        return OAFrame_FixedCaravanUtility.RemoveThingsOfDef(fixedCaravan: fixedCaravan,
                                                            thingDef: OARO_ThingDefOf.OARO_OrderRecommendation,
                                                            count: useCount,
                                                            validator: (t) => ((OrderRecommendation)t).RatkinOrder == order);
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
            < 30 => 5,
            < 70 => 4,
            < 90 => 3,
            _ => 2
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
}
