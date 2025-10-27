using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderHallHandler : IExposable
{
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
                nextHallRoomCacheTick = Find.TickManager.TicksGame + 250;
                orderHallRoom = mainOrderCodePedestal?.GetRoom();
            }
            return orderHallRoom;
        }
    }

    [Unsaved]
    private static SimpleValueCache<int> orderHallLevelCache = new(cacheInterval: 2500,
                                                                   defaultValue: 0,
                                                                   checker: static delegate
                                                                   {
                                                                       return OrderHallUtility.GetOrderHallLevel(OrderHallRoom);
                                                                   });
    public static int OrderHallLevel => orderHallLevelCache.GetCachedResult();

    [Unsaved]
    private static SimpleValueCache<int> academicFurnituresCache = new(cacheInterval: 2500,
                                                                      defaultValue: 0,
                                                                      checker: static delegate
                                                                      {
                                                                          return OrderHallUtility.KnightAcademicFurnituresCount(OrderHallRoom);
                                                                      });
    public static int AcademicFurnituresCount => academicFurnituresCache.GetCachedResult();

    public OrderHallHandler() => ResetStaticValue();

    public static void ResetStaticValue()
    {
        mainOrderCodePedestal = null;
        nextHallRoomCacheTick = -1;
        orderHallLevelCache.Reset();
        academicFurnituresCache.Reset();
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

    public static void OnPedestalChange()
    {
        nextHallRoomCacheTick = -1;
        orderHallLevelCache.Reset();
        academicFurnituresCache.Reset();
    }
}