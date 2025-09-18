using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class RatkinOrderManager : IExposable, IPostLoadInit
{
    public static RatkinOrderManager Instance { get; private set; }

    private List<RatkinOrder> allRatkinOrders = [];
    public IReadOnlyList<RatkinOrder> AllRatkinOrders => allRatkinOrders;

    public RatkinOrderManager()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(Instance));
        Instance = this;
    }
    public static void ClearStaticCache() => Instance = null;

    public void OpenDevWindow()
    {
        Find.WindowStack.Add(new DevWindow_AllOrders());
    }

    public void Tick()
    {
        for (int i = 0; i < allRatkinOrders.Count; i++)
        {
            allRatkinOrders[i].Tick();
        }
    }

    public bool IsFactionHasRatkinOrder(Faction faction)
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

    public RatkinOrder GetRatkinOrderForFaction(Faction faction)
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

    public void AddRatkinOrder(RatkinOrder order)
    {
        if (order is not null && !allRatkinOrders.Contains(order))
        {
            allRatkinOrders.Add(order);
        }
    }

    public void RemoveRatkinOrder(RatkinOrder order)
    {
        if (!allRatkinOrders.Contains(order))
        {
            return;
        }

        allRatkinOrders.Remove(order);
        order.Notify_Removed();

        GlobalOrderInteractionManager.Instance.Notify_RatkinOrderRemoved(order);
        MapComponent_RatkinOrder.OnRatkinOrderRemoved(order);
        Find.QuestManager.OnRatkinOrderRemoved(order);
    }

    public void PostLoadInit()
    {
        if (allRatkinOrders.RemoveAll(r => r is null) > 0)
        {
            Log.Error($"Some Ratkin Orders were null after loading and have been removed.");
        }
        for (int i = 0; i < allRatkinOrders.Count; i++)
        {
            allRatkinOrders[i].PostLoadInit();
        }
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref allRatkinOrders, "allRatkinOrders", LookMode.Deep);
    }
}
