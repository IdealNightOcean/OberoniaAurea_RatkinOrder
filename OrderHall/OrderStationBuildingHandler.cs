using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderStationBuildingHandler
{
    private const int BuildingsRecacheInterval = 30000;

    [Unsaved] private int nextHallBuildingCacheTick = -1;
    [Unsaved] private int academicFurnituresCount;
    public int AcademicFurnituresCount
    {
        get
        {
            if (Find.TickManager.TicksGame > nextHallBuildingCacheTick)
            {
                RecacheOrderHallBuildings();
            }
            return academicFurnituresCount;
        }
    }

    [Unsaved] private readonly Dictionary<KnightPersonality, HashSet<ThingDef>> preferBuildingDefsByKnightPersonality = new(KnightPersonalityExtension.AvailablePersonalitiesCount);
    public IReadOnlyDictionary<KnightPersonality, HashSet<ThingDef>> KnightBuildingDefsByPersonality
    {
        get
        {
            if (Find.TickManager.TicksGame > nextHallBuildingCacheTick)
            {
                RecacheOrderHallBuildings();
            }
            return preferBuildingDefsByKnightPersonality;
        }
    }

    private void RecacheOrderHallBuildings()
    {
        nextHallBuildingCacheTick = Find.TickManager.TicksGame + BuildingsRecacheInterval;

        academicFurnituresCount = 0;
        preferBuildingDefsByKnightPersonality.Clear();
        try
        {
            Room room = OrderStationHandler.Instance.OrderHallRoom;
            if (room is null)
            {
                return;
            }
            HashSet<Thing> uniquePotentialBuildings = new(32);
            foreach (Region region in room.Regions)
            {
                List<Thing> allThings = region.ListerThings.AllThings;
                for (int i = 0; i < allThings.Count; i++)
                {
                    ThingDef thingDef = allThings[i].def;
                    if (thingDef.building is null || thingDef.building.buildingTags is null)
                        continue;

                    if (!uniquePotentialBuildings.Add(allThings[i]))
                        continue;

                    foreach (string tag in thingDef.building.buildingTags)
                    {
                        if (tag == "OARO_KnightAcademic")
                        {
                            academicFurnituresCount++;
                        }
                        else if (tag == "OARO_ResidentKnightPrefer")
                        {
                            if (OrderDefDataBase.TryGetKnightPersonalityByBuilding(thingDef, out KnightPersonality personality))
                            {
                                if (preferBuildingDefsByKnightPersonality.TryGetValue(personality, out HashSet<ThingDef> defsHash))
                                {
                                    defsHash.Add(thingDef);
                                }
                                else
                                {
                                    preferBuildingDefsByKnightPersonality.Add(personality, [thingDef]);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"重新缓存分部大厅建筑",
                typeName: nameof(OrderStationHandler),
                methodName: nameof(RecacheOrderHallBuildings),
                needStackTrace: true);
        }
    }

    public void RefreshCache()
    {
        academicFurnituresCount = 0;
        nextHallBuildingCacheTick = -1;
        preferBuildingDefsByKnightPersonality.Clear();
    }
}