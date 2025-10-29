using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class OrderHallUtility
{
    private static readonly int[] areaBoundaries = [int.MinValue, 40, 50, 60, 80, 120, 160];
    private static readonly float[] impressivenessBoundaries = [float.MinValue, 80f, 90f, 120f, 140f, 160f, 190f];
    private static OrderHallRestrictionExtension restrictionExtension;
    private static OrderHallRestrictionExtension RestrictionExtension => restrictionExtension ??= OARO_ModDefOf.OARO_RatkinOrderHall.GetModExtension<OrderHallRestrictionExtension>();

    public static int GetOrderHallLevel(Room room)
    {
        int maxPotentialLevel = 0;
        try
        {
            if (room is null || room != OrderHallHandler.OrderHallRoom)
            {
                return 0;
            }

            int areaRestrict = Array.BinarySearch(areaBoundaries, room.CellCount);
            areaRestrict = areaRestrict < 0 ? ~areaRestrict : areaRestrict + 1;
            int impressivenessRestrict = Array.BinarySearch(impressivenessBoundaries, room.GetStat(RoomStatDefOf.Impressiveness));
            impressivenessRestrict = impressivenessRestrict < 0 ? ~impressivenessRestrict : impressivenessRestrict + 1;

            maxPotentialLevel = Mathf.Min(areaRestrict, impressivenessRestrict, 7);
            maxPotentialLevel = maxPotentialLevel < 1 ? 1 : maxPotentialLevel;

            if (maxPotentialLevel <= 1) { return 1; }

            Log.Message($"before terrain: {maxPotentialLevel}");
            maxPotentialLevel = TerrainRestrict(room, maxPotentialLevel);
            // 最高可能索引为0，只能是1级
            if (maxPotentialLevel <= 1) { return 1; }

            Log.Message($"before building: {maxPotentialLevel}");
            maxPotentialLevel = BuildingRestrict(room, maxPotentialLevel);

            return Mathf.Clamp(maxPotentialLevel, 1, 7);
        }
        catch (Exception ex)
        {
            Log.Error($"Exception occurred on {nameof(OrderHallUtility)}.{nameof(GetOrderHallLevel)}.\nException:\n{ex.Message}");
            return maxPotentialLevel;
        }
    }

    private static int TerrainRestrict(Room room, int maxPotentialLevel)
    {
        Map map = room.Map;
        foreach (IntVec3 cell in room.Cells)
        {
            List<string> terrainTags = cell.GetTerrain(map).tags;

            // 无地板最高1级
            if (terrainTags.NullOrEmpty())
            {
                return 1;
            }

            if (maxPotentialLevel <= 4)
            {
                // 无地板最高1级
                if (!terrainTags.Contains("Floor"))
                {
                    return 1;
                }
            }
            else if (maxPotentialLevel <= 6)
            {
                if (!terrainTags.Contains("FineFloor"))
                {
                    // 无精致地板，最高4级
                    maxPotentialLevel = 4;
                    if (!terrainTags.Contains("Floor"))
                    {
                        return 1;
                    }
                }
            }
            else
            {
                if (!terrainTags.Contains("OARO_OrderFloor"))
                {
                    // 无骑士团精致地板，最高6级
                    maxPotentialLevel = 6;
                    if (!terrainTags.Contains("FineFloor"))
                    {
                        // 无精致地板，最高4级
                        maxPotentialLevel = 4;
                        if (!terrainTags.Contains("Floor"))
                        {
                            // 无地板最高1级
                            return 1;
                        }
                    }
                }
            }
        }

        return maxPotentialLevel;
    }

    private static int BuildingRestrict(Room room, int maxPotentialLevel)
    {
        HashSet<string> forbiddenBuildingTags = RestrictionExtension.ForbiddenBuildingTags;
        Dictionary<ThingDef, int> orderHallBuildings = [];

        foreach (Region region in room.Regions)
        {
            List<Thing> allThings = region.ListerThings.AllThings;
            for (int i = 0; i < allThings.Count; i++)
            {
                ThingDef thingDef = allThings[i].def;

                // 有祭坛最高1级
                if (thingDef.isAltar)
                {
                    return 1;
                }

                if (thingDef.building is null || thingDef.building.buildingTags is null)
                {
                    continue;
                }

                bool isPotentialBuilding = false;
                foreach (string tag in thingDef.building.buildingTags)
                {
                    // 有禁用类型建筑最高 1级
                    if (forbiddenBuildingTags.Contains(tag))
                    {
                        return 1;
                    }

                    if (tag == "OARO_OrderHall")
                    {
                        isPotentialBuilding = true;
                    }
                }

                if (isPotentialBuilding)
                {
                    if (orderHallBuildings.TryGetValue(thingDef, out int count))
                    {
                        orderHallBuildings[thingDef] = count + 1;
                    }
                    else
                    {
                        orderHallBuildings[thingDef] = 1;
                    }
                }
            }
        }


        for (int i = maxPotentialLevel - 1; i >= 1; i--)
        {
            List<ThingDefCountClass> buildingRequirements = RestrictionExtension.buildingRequirements[i].buildings;
            bool allMet = true;
            for (int j = 0; j < buildingRequirements.Count; j++)
            {
                if (!orderHallBuildings.TryGetValue(buildingRequirements[j].thingDef, out int count) || count < buildingRequirements[j].count)
                {
                    maxPotentialLevel = i;
                    allMet = false;
                    break;
                }
            }
            if (allMet)
            {
                break;
            }
        }

        return maxPotentialLevel;
    }
}