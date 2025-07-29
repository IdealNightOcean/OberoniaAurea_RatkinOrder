using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class RatkinOrderManager : IExposable, IPostLoadInit
{
    public static RatkinOrderManager Instance { get; private set; }

    private List<RatkinOrder> allRatkinOrders = [];
    public List<RatkinOrder> AllRatkinOrders => allRatkinOrders;

    public RatkinOrderManager()
    {
        Instance = this;
    }

    public bool IsFactionHasRatkinOrder(Faction faction)
    {
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
    }

    public void PostLoadInit()
    {
        if (allRatkinOrders.RemoveAll(r => r is null) > 0)
        {
            Log.Error($"Some Ratkin Orders were null after loading and have been removed.");
        }
        allRatkinOrders.ForEach(r => r.PostLoadInit());
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref allRatkinOrders, "allRatkinOrders", LookMode.Deep);
    }
}
