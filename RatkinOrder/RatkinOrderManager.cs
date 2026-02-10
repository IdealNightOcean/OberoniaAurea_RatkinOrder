using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class RatkinOrderManager : IExposable
{
    public static RatkinOrderManager Instance { get; private set; }

    private List<RatkinOrder> allRatkinOrders = [];
    public IReadOnlyList<RatkinOrder> AllRatkinOrders => allRatkinOrders;
    public int RatkinOrdersCount => allRatkinOrders.Count;

    public RatkinOrderManager()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(AroundKnightGroupsManager));
        Instance = this;
    }
    public static void ClearStaticCache() => Instance = null;
    public void ExposeData()
    {
        Scribe_Collections.Look(ref allRatkinOrders, nameof(allRatkinOrders), LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            PostLoadInit();
        }
    }

    public static void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_AllOrders());

    public void Tick()
    {
        for (int i = 0; i < allRatkinOrders.Count; i++)
        {
            allRatkinOrders[i].Tick();
        }
    }

    public bool FactionHasRatkinOrder(Faction faction)
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
        order.OnRemoved();

        GlobalInteractionManager.Instance.Notify_RatkinOrderRemoved(order);
        MapComponent_RatkinOrder.OnRatkinOrderRemoved(order);
        Find.QuestManager.OnRatkinOrderRemoved(order);
    }

    private void PostLoadInit()
    {
        if (allRatkinOrders.RemoveAll(r => !r.IsValid()) > 0)
        {
            Log.Error($"[OARO] 部分骑士团在加载后失效，已被移除。");
        }
        for (int i = 0; i < allRatkinOrders.Count; i++)
        {
            allRatkinOrders[i].PostLoadInit();
        }
    }
}
