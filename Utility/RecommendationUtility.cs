using OberoniaAurea_Frame;
using RimWorld;
using System.Runtime.CompilerServices;
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
        if (map is null)
            return 0;

        int totalCount = 0;
        foreach (Thing t in map.listerThings.ThingsOfDef(OARO_ThingDefOf.OARO_OrderRecommendation))
            totalCount += t.stackCount;

        return totalCount;
    }

    public static bool HasEnoughRecommendation(IThingHolder thingHolder, int count) => OAFrame_ThingUtility.HasEnoughThingsOfDef(thingHolder, OARO_ThingDefOf.OARO_OrderRecommendation, count);

    public static bool GiveRecommendationsToPlayer(IThingHolder thingHolder, int count, bool sendStandLetter = true, RatkinOrder ratkinOrder = null)
    {
        if (count <= 0 || thingHolder is null)
            return false;

        OrderRecommendation recommendations = MakeRecommendationForPlayer(count);

        if (!OAFrame_ThingUtility.GiveThingToPlayer(recommendations, thingHolder))
            return false;

        if (sendStandLetter)
        {
            if (thingHolder is Map)
                SendStandardGetRecommendationLetter(count, ratkinOrder, recommendations);

            else if (thingHolder is LookTargets lookTargets)
                SendStandardGetRecommendationLetter(count, ratkinOrder, lookTargets);
        }
        return true;
    }

    public static bool GiveRecommendationsToPlayerMap(Map map, int count, bool sendStandLetter = true, RatkinOrder ratkinOrder = null, IntVec3? spawnCell = null, bool dropPod = false)
    {
        if (map is null || count <= 0)
            return false;

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
                spawnCell = cell.IsValid ? cell : map.Center;
            }
            GenPlace.TryPlaceThing(recommendations, spawnCell.Value, map, ThingPlaceMode.Near);
        }

        if (sendStandLetter)
        {
            SendStandardGetRecommendationLetter(count, ratkinOrder, new LookTargets(spawnCell.Value, map));
        }
        return true;
    }

    /// <returns>实际使用数</returns>
    public static int UseRecommendationOfPlayer(IThingHolder thingHolder, int useCount) => OAFrame_ThingUtility.RemoveThingsOfDef(thingHolder, OARO_ThingDefOf.OARO_OrderRecommendation, useCount);

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
    public static int RecommendationNeed_ResidentKnightRankUpgrade(RatkinOrder ratkinOrder, ResidentKnightRank targetRank)
    {
        int needCount = targetRank switch
        {
            ResidentKnightRank.Regular => 0,
            ResidentKnightRank.Elite => 1,
            ResidentKnightRank.Honor => 2,
            ResidentKnightRank.Crown => 4,
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
                                      : "OARO_Letter_GetRecommendation_HasOrder".Translate(ratkinOrder.NameColored.Named(OARO_KeyLibrary_FormatArgName.OrderName), count.Named(KeyLibrary_FormatArgName.Count)),
            def: OARO_LetterDefOf.OARO_Order_PositiveLetter,
            lookTargets: lookTargets,
            relatedFaction: ratkinOrder?.Faction);
        letter.RelatedOrder = ratkinOrder;
        Find.LetterStack.ReceiveLetter(letter);
    }
}
