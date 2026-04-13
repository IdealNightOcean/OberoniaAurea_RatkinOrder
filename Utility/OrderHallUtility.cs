using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class OrderHallUtility
{
    private static OrderHallRestrictionExtension restrictionExtension;
    private static OrderHallRestrictionExtension RestrictionExtension
    {
        get
        {
            if (restrictionExtension is null)
            {
                restrictionExtension = OARO_ModDefOf.OARO_RatkinOrderHall.GetModExtension<OrderHallRestrictionExtension>();
                restrictionExtension?.hallLevelRestriction.OrderBy(r => r.level);
            }
            return restrictionExtension;
        }
    }

    public static int GetOrderHallLevel()
    {
        int maxOrderHallLevel = RestrictionExtension.MaxLevel;
        int maxPotentialLevel = 0;
        try
        {
            Room room = OrderStationHandler.Instance.OrderHallRoom;
            if (room is null)
            {
                return -1;
            }

            int areaRestrict = AreaRestrict(room.CellCount);
            int impressivenessRestrict = ImpressivenessRestrict(room.GetStat(RoomStatDefOf.Impressiveness));
            maxPotentialLevel = Mathf.Min(areaRestrict, impressivenessRestrict, maxOrderHallLevel);
            if (maxPotentialLevel <= 1) { return 1; }

            maxPotentialLevel = TerrainRestrict(room, maxPotentialLevel);
            if (maxPotentialLevel <= 1) { return 1; }

            maxPotentialLevel = BuildingRestrict(room, maxPotentialLevel);
            return Mathf.Clamp(maxPotentialLevel, 1, maxOrderHallLevel);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"获取骑士大厅等级",
                typeName: nameof(OrderHallUtility),
                methodName: nameof(GetOrderHallLevel),
                needStackTrace: true);
            return maxPotentialLevel;
        }
    }

    public static HashSet<ThingDef> GetAllResidentKnightPreferredBuildingDefs(Room room)
    {
        HashSet<ThingDef> allBuildingDefs = [];
        if (room is null)
        {
            return allBuildingDefs;
        }

        foreach (Region region in room.Regions)
        {
            List<Thing> allThings = region.ListerThings.AllThings;
            for (int i = 0; i < allThings.Count; i++)
            {
                List<string> buildingTags = allThings[i].def.building?.buildingTags;
                if (buildingTags is null)
                {
                    continue;
                }

                if (buildingTags.Contains("OARO_ResidentKnightPrefer"))
                {
                    allBuildingDefs.Add(allThings[i].def);
                }
            }
        }

        return allBuildingDefs;
    }

    private static int AreaRestrict(int cellCount)
    {
        List<OrderHallLevelRestriction> hallLevelRestriction = RestrictionExtension.hallLevelRestriction;
        for (int i = 0; i < hallLevelRestriction.Count; i++)
        {
            if (cellCount < hallLevelRestriction[i].areaFloor)
            {
                return i + 1;
            }
        }
        return RestrictionExtension.MaxLevel;
    }

    private static int ImpressivenessRestrict(float impressiveness)
    {
        List<OrderHallLevelRestriction> hallLevelRestriction = RestrictionExtension.hallLevelRestriction;
        for (int i = 0; i < hallLevelRestriction.Count; i++)
        {
            if (impressiveness < hallLevelRestriction[i].impressivenessFloor)
            {
                return i + 1;
            }
        }
        return RestrictionExtension.MaxLevel;
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
        HashSet<Thing> uniquePotentialBuildings = new(32);
        Dictionary<ThingDef, int> orderHallBuildings = new(8);

        foreach (Region region in room.Regions)
        {
            List<Thing> allThings = region.ListerThings.AllThings;
            for (int i = 0; i < allThings.Count; i++)
            {
                ThingDef thingDef = allThings[i].def;
                // 有祭坛最高1级
                if (thingDef.isAltar)
                    return 1;

                if (thingDef.building is null || thingDef.building.buildingTags is null)
                    continue;

                if (!uniquePotentialBuildings.Add(allThings[i]))
                    continue;

                bool isPotentialBuilding = false;
                foreach (string tag in thingDef.building.buildingTags)
                {
                    // 有禁用类型建筑最高 1级
                    if (forbiddenBuildingTags.Contains(tag))
                    {
                        return 1;
                    }

                    isPotentialBuilding = isPotentialBuilding || tag == "OARO_OrderHall";
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

        int prePotentialLevelIndex = maxPotentialLevel - 1;
        for (int i = prePotentialLevelIndex; i >= 1; i--)
        {
            List<ThingDefCountClass> buildingRequirements = RestrictionExtension.hallLevelRestriction[i].buildings;
            bool allMet = true;
            for (int j = 0; j < buildingRequirements.Count; j++)
            {
                if (!orderHallBuildings.TryGetValue(buildingRequirements[j].thingDef, out int count) || count < buildingRequirements[j].count)
                {
                    maxPotentialLevel--;
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

    public static List<(string condition, bool isMet)> GetHallUpgradeInfo()
    {
        int curLevel = GetOrderHallLevel();
        if (curLevel < 0 || curLevel >= RestrictionExtension.MaxLevel)
            return null;


        Room room = OrderStationHandler.Instance.OrderHallRoom;
        OrderHallLevelRestriction nextLevelRestriction = RestrictionExtension.GetRestrictionOfLevel(curLevel + 1);
        if (nextLevelRestriction is null)
            return null;

        List<(string condition, bool isMet)> result = new(16)
        {
            ("OARO_HallRestriction_OrderCodePedestal".Translate(), curLevel >= 1)
        };

        int cellCount = room.CellCount;
        result.Add(("OARO_HallRestriction_Area".Translate(cellCount, nextLevelRestriction.areaFloor), cellCount >= nextLevelRestriction.areaFloor));

        float impressiveness = room.GetStat(RoomStatDefOf.Impressiveness);
        result.Add(("OARO_HallRestriction_Impressiveness".Translate(impressiveness.ToString("F0"), nextLevelRestriction.impressivenessFloor.ToString("F0")), impressiveness >= nextLevelRestriction.impressivenessFloor));

        int targetLevel = curLevel + 1;
        Map map = room.Map;
        bool terrainMet = true;
        string terrainTag = targetLevel <= 4 ? "Floor" : (targetLevel <= 6 ? "FineFloor" : "OARO_OrderFloor");
        foreach (IntVec3 cell in room.Cells)
        {
            List<string> terrainTags = cell.GetTerrain(map).tags;

            if (terrainTags.NullOrEmpty())
            {
                terrainMet = false;
                break;
            }

            if (!terrainTags.Contains(terrainTag))
            {
                terrainMet = false;
                break;
            }
        }
        result.Add(($"OARO_TerrainTag_{terrainTag}".Translate(), terrainMet));

        HashSet<string> forbiddenBuildingTags = RestrictionExtension.ForbiddenBuildingTags;

        HashSet<Thing> uniquePotentialBuildings = new(32);
        HashSet<string> containedForbiddenBuildingTags = new(8);
        Dictionary<ThingDef, int> orderHallBuildings = new(8);
        bool hasAltar = false;
        foreach (Region region in room.Regions)
        {
            List<Thing> allThings = region.ListerThings.AllThings;
            for (int i = 0; i < allThings.Count; i++)
            {
                ThingDef thingDef = allThings[i].def;

                if (thingDef.isAltar)
                    hasAltar = true;

                if (thingDef.building is null || thingDef.building.buildingTags is null)
                    continue;

                if (!uniquePotentialBuildings.Add(allThings[i]))
                    continue;

                bool isPotentialBuilding = false;
                foreach (string tag in thingDef.building.buildingTags)
                {
                    if (forbiddenBuildingTags.Contains(tag))
                    {
                        containedForbiddenBuildingTags.Add(tag);
                    }

                    isPotentialBuilding = isPotentialBuilding || tag == "OARO_OrderHall";
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

        result.Add(("OARO_ForbiddenBuilding_Altar".Translate(), !hasAltar));
        foreach (string tag in forbiddenBuildingTags)
        {
            result.Add(($"OARO_ForbiddenBuildingTag_{tag}".Translate(), !containedForbiddenBuildingTags.Contains(tag)));
        }

        if (!nextLevelRestriction.buildings.NullOrEmpty())
        {
            foreach (ThingDefCountClass buildingNeeded in nextLevelRestriction.buildings)
            {
                int currentCount = orderHallBuildings.TryGetValue(buildingNeeded.thingDef, out int count) ? count : 0;
                result.Add((($"{buildingNeeded.LabelCap}: {currentCount} / {buildingNeeded.count}", currentCount >= buildingNeeded.count)));
            }
        }

        return result;
    }
}