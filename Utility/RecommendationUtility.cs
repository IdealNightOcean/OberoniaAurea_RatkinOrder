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

public static class RecommendationUtility
{
    public static bool IsRecommendationOfOrder(this Thing t, RatkinOrder order)
    {
        if (t is null || t.def != OARO_ThingDefOf.OARO_OrderRecommendation)
        {
            return false;
        }

        return ((OrderRecommendation)t).Order == order;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CurRecommendationOfMap(RatkinOrder order, Map map)
    {
        return map?.listerThings.ThingsOfDef(OARO_ThingDefOf.OARO_OrderRecommendation)?.Cast<OrderRecommendation>().Where(r => r.Order == order).Count() ?? 0;
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

    public static void GiveRecommendationsToPlayer_Map(RatkinOrder order, int count, Map map, IntVec3? spawnCell, bool drop = false)
    {
        GiveRecommendationsToPlayer(order, count, MapGiveAction);

        void MapGiveAction(Thing recommendations)
        {
            if (drop)
            {
                spawnCell ??= DropCellFinder.TradeDropSpot(map);
                DropPodUtility.DropThingsNear(spawnCell.Value, map, [recommendations], allowFogged: false, faction: order.Faction);
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
            validator: (t) => ((OrderRecommendation)t).Order == order,
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
                                                        validator: (t) => ((OrderRecommendation)t).Order == order);

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
                                                            validator: (t) => ((OrderRecommendation)t).Order == order);
    }
}
