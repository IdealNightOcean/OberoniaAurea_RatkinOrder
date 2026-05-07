using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士驻地处理器 - 负责管理骑士驻地相关的全局状态和缓存，包括主 <see cref="Building_OrderCodePedestal"/> 的引用、驻地所在房间、驻地等级、驻地建筑等信息的缓存和更新
/// </summary>
public class OrderStationHandler : IExposable
{
    private const int StationRoomRecacheInterval = 250;
    private const int StationLevelRecacheInterval = 15000;
    private const int StationBuildingsRecacheInterval = 30000;

    public static OrderStationHandler Instance { get; private set; }

    private Building_OrderCodePedestal mainOrderCodePedestal;
    public Building_OrderCodePedestal MainOrderCodePedestal => mainOrderCodePedestal;

    [Unsaved] private Room orderStationRoom;
    [Unsaved] private int nextStationRoomCacheTick = -1;
    public Room OrderStationRoom
    {
        get
        {
            if (Find.TickManager.TicksGame > nextStationRoomCacheTick)
            {
                nextStationRoomCacheTick = Find.TickManager.TicksGame + StationRoomRecacheInterval;
                orderStationRoom = mainOrderCodePedestal?.GetRoom();
            }
            return orderStationRoom;
        }
    }
    public Map OrderStationMap => OrderStationRoom?.Map;

    [Unsaved]
    private SimpleValueCache<int> orderStationLevelCache;
    public int OrderStationLevel => orderStationLevelCache.GetCachedResult();

    private OrderStationBuildingHandler buildingHandler;
    public static OrderStationBuildingHandler BuildingHandler => Instance.buildingHandler;

    private OrderStationTraditionsManager traditionsManager;
    public static OrderStationTraditionsManager TraditionsManager => Instance.traditionsManager;

    internal OrderStationHandler(bool initCtor)
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(OrderStationHandler));
        Instance = this;
        orderStationLevelCache = new SimpleValueCache<int>(cacheInterval: StationLevelRecacheInterval,
                                                        defaultValue: 0,
                                                        checker: () => OrderStationUtility.GetOrderStationLevel());

        buildingHandler = new();
        if (initCtor)
        {

        }
    }

    public static void ClearStaticCache() => Instance = null;

    public void RefreshCache()
    {
        nextStationRoomCacheTick = -1;
        orderStationLevelCache.Reset();
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