using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class RatkinOrderManager : IExposable
{
    private static List<RatkinOrder> allRatkinOrders = [];
    public static IReadOnlyList<RatkinOrder> AllRatkinOrders => allRatkinOrders;

    public RatkinOrderManager()
    {
        allRatkinOrders.Clear();
    }
    public static void ClearStaticCache()
    {
        allRatkinOrders.Clear();
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref allRatkinOrders, "allRatkinOrders", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            PostLoadInit();
        }
    }

    public static void OpenDevWindow()
    {
        Find.WindowStack.Add(new DevWindow_AllOrders());
    }

    public static void Tick()
    {
        for (int i = 0; i < allRatkinOrders.Count; i++)
        {
            allRatkinOrders[i].Tick();
        }
    }

    public static bool FactionHasRatkinOrder(Faction faction)
    {
        if (faction is null)
        {
            return false;
        }

        for (int i = 0; i < allRatkinOrders.Count; i++)
        {
            if (allRatkinOrders[i].Faction == faction)
            {
                return true;
            }
        }
        return false;
    }

    public static RatkinOrder GetRatkinOrderForFaction(Faction faction)
    {
        if (faction is null)
        {
            return null;
        }

        for (int i = 0; i < allRatkinOrders.Count; i++)
        {
            if (allRatkinOrders[i].Faction == faction)
            {
                return allRatkinOrders[i];
            }
        }
        return null;
    }

    public static void AddRatkinOrder(RatkinOrder order)
    {
        if (order is not null && !allRatkinOrders.Contains(order))
        {
            allRatkinOrders.Add(order);
        }
    }

    public static void RemoveRatkinOrder(RatkinOrder order)
    {
        if (!allRatkinOrders.Contains(order))
        {
            return;
        }

        allRatkinOrders.Remove(order);
        order.OnRemoved();

        GlobalInteractionManager.Instance.Notify_RatkinOrderRemoved(order);
        MapComponent_RatkinOrder.OnRatkinOrderRemoved(order);
        Find.QuestManager.OnRatkinOrderRemoved(order);
    }

    private static void PostLoadInit()
    {
        if (allRatkinOrders.RemoveAll(r => r is null) > 0)
        {
            Log.Error($"[OARO] Some Ratkin Orders were null after loading and have been removed.");
        }
        for (int i = 0; i < allRatkinOrders.Count; i++)
        {
            allRatkinOrders[i].PostLoadInit();
        }
    }
}
