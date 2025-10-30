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

    private static OrderCodePedestal mainOrderCodePedestal;
    public static OrderCodePedestal MainOrderCodePedestal => mainOrderCodePedestal;

    [Unsaved] private static Room orderHallRoom;
    [Unsaved] private static int nextHallRoomCacheTick = -1;
    public static Room OrderHallRoom
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
    private static readonly SimpleValueCache<int> orderHallLevelCache = new(cacheInterval: HallLevelRecacheInterval,
                                                                   defaultValue: 0,
                                                                   checker: static delegate
                                                                   {
                                                                       return OrderHallUtility.GetOrderHallLevel(OrderHallRoom);
                                                                   });
    public static int OrderHallLevel => orderHallLevelCache.GetCachedResult();

    [Unsaved] private static int nextHallBuildingCacheTick = -1;
    [Unsaved] private static int academicFurnituresCount;
    public static int AcademicFurnituresCount
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

    [Unsaved] private static readonly Dictionary<KnightPersonality, HashSet<ThingDef>> knightJoyBuildingDefsByPersonality = new(EnumArraryLibrary.AvailablePersonalitiesCount);
    public static IReadOnlyDictionary<KnightPersonality, HashSet<ThingDef>> KnightJoyBuildingDefsByPersonality
    {
        get
        {
            if (Find.TickManager.TicksGame > nextHallBuildingCacheTick)
            {
                RecacheOrderHallBuildings();
            }
            return knightJoyBuildingDefsByPersonality;
        }
    }

    public OrderHallHandler() => ResetStaticValue();

    public static void ResetStaticValue()
    {
        mainOrderCodePedestal = null;
        OnPedestalChange();
    }
    public static void OnPedestalChange()
    {
        academicFurnituresCount = 0;
        nextHallRoomCacheTick = -1;
        nextHallBuildingCacheTick = -1;
        orderHallLevelCache.Reset();
        knightJoyBuildingDefsByPersonality.Clear();
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref mainOrderCodePedestal, "mainOrderCodePedestal");
    }

    public static bool TrySetMainPedestal(OrderCodePedestal pedestal, bool replaceCur)
    {
        if (pedestal is null)
        {
            Log.Error("Cannot set null OrderCodePedestal as main OrderCodePedestal.");
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
        OnPedestalChange();
        return true;
    }

    public static bool TryUnsetMainPedestal(OrderCodePedestal pedestal)
    {
        if (pedestal is null || pedestal != mainOrderCodePedestal)
        {
            return false;
        }
        mainOrderCodePedestal = null;
        Messages.Message("OARO_MainOrderCodePedestal_Unset".Translate(), MessageTypeDefOf.NeutralEvent, historical: false);
        OnPedestalChange();
        return true;
    }

    private static void RecacheOrderHallBuildings()
    {
        nextHallBuildingCacheTick = Find.TickManager.TicksGame + HallBuildingsRecacheInterval;

        academicFurnituresCount = 0;
        knightJoyBuildingDefsByPersonality.Clear();
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

                    if (buildingTags.Contains("OARO_KnightJoyFurniture"))
                    {
                        ThingDef thingDef = allThings[i].def;
                        if (OrderDefDataBase.GetKnightPersonalityForJoyBuilding(thingDef, out KnightPersonality personality))
                        {
                            if (knightJoyBuildingDefsByPersonality.TryGetValue(personality, out HashSet<ThingDef> defsHash))
                            {
                                defsHash.Add(thingDef);
                            }
                            else
                            {
                                knightJoyBuildingDefsByPersonality.Add(personality, [thingDef]);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Exception occurred on {nameof(OrderHallHandler)}.{nameof(RecacheOrderHallBuildings)}.\nException:\n{ex.Message}");
        }
    }
}