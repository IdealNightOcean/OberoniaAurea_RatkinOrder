using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderStationHandler : IExposable
{
    private const int HallRoomRecacheInterval = 250;
    private const int HallLevelRecacheInterval = 15000;
    private const int HallBuildingsRecacheInterval = 30000;

    public static OrderStationHandler Instance { get; private set; }

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
    private SimpleValueCache<int> orderHallLevelCache;
    public int OrderHallLevel => orderHallLevelCache.GetCachedResult();

    private OrderStationBuildingHandler buildingHandler;
    public static OrderStationBuildingHandler BuildingHandler => Instance.buildingHandler;

    private OrderStationTraditionsManager traditionsManager;
    public static OrderStationTraditionsManager TraditionsManager => Instance.traditionsManager;

    internal OrderStationHandler(bool initCtor)
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(OrderStationHandler));
        Instance = this;
        orderHallLevelCache = new SimpleValueCache<int>(cacheInterval: HallLevelRecacheInterval,
                                                        defaultValue: 0,
                                                        checker: () => OrderHallUtility.GetOrderHallLevel());

        buildingHandler = new();
        if (initCtor)
        {

        }
    }

    public static void ClearStaticCache() => Instance = null;

    public void RefreshCache()
    {
        nextHallRoomCacheTick = -1;
        orderHallLevelCache.Reset();
        buildingHandler.RefreshCache();
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref mainOrderCodePedestal, nameof(mainOrderCodePedestal));
        Scribe_Deep.Look(ref buildingHandler, nameof(buildingHandler));
        Scribe_Deep.Look(ref traditionsManager, nameof(traditionsManager));
    }

    public bool TrySetMainPedestal(Building_OrderCodePedestal pedestal, bool replaceCur)
    {
        if (pedestal is null)
        {
            Log.Error("[OARO] 无法将 null 设置为主 OrderCodePedestal。");
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

}