using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderHallHandler : IExposable
{
    private const int HallRoomRecacheInterval = 250;
    private const int HallLevelRecacheInterval = 15000;
    private const int HallBuildingsRecacheInterval = 30000;

    public static OrderHallHandler Instance { get; private set; }

    private Building_OrderCodePedestal mainOrderCodePedestal;
    public Building_OrderCodePedestal MainOrderCodePedestal => mainOrderCodePedestal;

    [Unsaved] private Room orderHallRoom;
    [Unsaved] private int nextHallRoomCacheTick = -1;
    public Room OrderHallRoom
    {
        get
        {
            if (Find.TickManager.TicksGame > nextHallRoomCacheTick)
            {
                nextHallRoomCacheTick = Find.TickManager.TicksGame + HallRoomRecacheInterval;
                orderHallRoom = mainOrderCodePedestal?.GetRoom();
            }
            return orderHallRoom;
        }
    }

    [Unsaved]
    private readonly SimpleValueCache<int> orderHallLevelCache;
    public int OrderHallLevel => orderHallLevelCache.GetCachedResult();

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

    [Unsaved] private readonly Dictionary<KnightPersonality, HashSet<ThingDef>> preferBuildingDefsByKnightPersonality = new(EnumArraryLibrary.AvailablePersonalitiesCount);
    public IReadOnlyDictionary<KnightPersonality, HashSet<ThingDef>> KnightJoyBuildingDefsByPersonality
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

    public OrderHallHandler()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(OrderHallHandler));
        Instance = this;
        orderHallLevelCache = new(cacheInterval: HallLevelRecacheInterval,
                                  defaultValue: 0,
                                  checker: () => OrderHallUtility.GetOrderHallLevel(OrderHallRoom));
    }
    public static void ClearStaticCache() => Instance = null;

    public void RefreshCache()
    {
        academicFurnituresCount = 0;
        nextHallRoomCacheTick = -1;
        nextHallBuildingCacheTick = -1;
        orderHallLevelCache.Reset();
        preferBuildingDefsByKnightPersonality.Clear();
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref mainOrderCodePedestal, nameof(mainOrderCodePedestal));
    }

    public bool TrySetMainPedestal(Building_OrderCodePedestal pedestal, bool replaceCur)
    {
        if (pedestal is null)
        {
            Log.Error("[OARO] Cannot set null OrderCodePedestal as main OrderCodePedestal.");
            return false;
        }
        if (pedestal == mainOrderCodePedestal)
        {
            return true;
        }
        if (mainOrderCodePedestal is not null && !replaceCur)
        {
            Messages.Message("OARO_MainOrderCodePedestal_RejectReplace".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }

        mainOrderCodePedestal = pedestal;
        RefreshCache();
        return true;
    }

    public bool TryUnsetMainPedestal(Building_OrderCodePedestal pedestal)
    {
        if (pedestal is null || pedestal != mainOrderCodePedestal)
        {
            return false;
        }
        mainOrderCodePedestal = null;
        Messages.Message("OARO_MainOrderCodePedestal_Unset".Translate(), MessageTypeDefOf.NeutralEvent, historical: false);
        RefreshCache();
        return true;
    }

    private void RecacheOrderHallBuildings()
    {
        nextHallBuildingCacheTick = Find.TickManager.TicksGame + HallBuildingsRecacheInterval;

        academicFurnituresCount = 0;
        preferBuildingDefsByKnightPersonality.Clear();
        try
        {
            Room room = OrderHallRoom;
            if (room is null)
            {
                return;
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
                    if (buildingTags.Contains("OARO_KnightAcademic"))
                    {
                        academicFurnituresCount++;
                    }

                    if (buildingTags.Contains("OARO_ResidentKnightPrefer"))
                    {
                        ThingDef thingDef = allThings[i].def;
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
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"recache order hall buildings",
                typeName: nameof(OrderHallHandler),
                methodName: nameof(RecacheOrderHallBuildings),
                needStackTrace: true);
        }
    }
}