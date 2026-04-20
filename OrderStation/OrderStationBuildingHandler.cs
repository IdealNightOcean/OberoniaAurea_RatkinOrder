using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士驻地建筑处理器 - 负责管理骑士驻地内的建筑相关信息，包括课业家具数量、各骑士精神偏好建筑等信息的缓存和更新
/// </summary>
public class OrderStationBuildingHandler
{
    private const int BuildingsRecacheInterval = 30000;

    [Unsaved] private int nextBuildingCacheTick = -1;
    [Unsaved] private int academicFurnituresCount;
    public int AcademicFurnituresCount
    {
        get
        {
            if (Find.TickManager.TicksGame > nextBuildingCacheTick)
            {
                RecacheBuildings();
            }
            return academicFurnituresCount;
        }
    }

    [Unsaved] private readonly Dictionary<KnightChivalryDef, HashSet<ThingDef>> preferBuildingDefsByChivalry = new(DefDatabase<KnightChivalryDef>.DefCount);
    public IReadOnlyDictionary<KnightChivalryDef, HashSet<ThingDef>> KnightBuildingDefsByChivalry
    {
        get
        {
            if (Find.TickManager.TicksGame > nextBuildingCacheTick)
            {
                RecacheBuildings();
            }
            return preferBuildingDefsByChivalry;
        }
    }

    private void RecacheBuildings()
    {
        nextBuildingCacheTick = Find.TickManager.TicksGame + BuildingsRecacheInterval;

        academicFurnituresCount = 0;
        preferBuildingDefsByChivalry.Clear();
        try
        {
            Room room = OrderStationHandler.Instance.OrderStationRoom;
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
                            KnightChivalryDef chivalry = thingDef.GetModExtension<ResidentKnightPreferredBuildingExtension>()?.chivalry;
                            if (chivalry is not null)
                            {
                                if (preferBuildingDefsByChivalry.TryGetValue(chivalry, out HashSet<ThingDef> defsHash))
                                {
                                    defsHash.Add(thingDef);
                                }
                                else
                                {
                                    preferBuildingDefsByChivalry.Add(chivalry, [thingDef]);
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
                methodName: nameof(RecacheBuildings),
                needStackTrace: true);
        }
    }

    public void RefreshCache()
    {
        academicFurnituresCount = 0;
        nextBuildingCacheTick = -1;
        preferBuildingDefsByChivalry.Clear();
    }
}