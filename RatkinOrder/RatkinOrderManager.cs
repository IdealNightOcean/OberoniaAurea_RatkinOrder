using OberoniaAurea_Frame;
using RimWorld;
using System;
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
        foreach (RatkinOrder ratkinOrder in allRatkinOrders)
        {
            try
            {
                ratkinOrder?.Tick();
            }
            catch (Exception ex)
            {
                if (Prefs.DevMode)
                {
                    Log.Error($"[OARO] 骑士团逻辑帧出现异常 {ratkinOrder.Name}（{ratkinOrder}）: {ex}");
                }
                else
                {
                    Log.ErrorOnce($"[OARO] 骑士团逻辑帧出现异常 {ratkinOrder.Name}（{ratkinOrder}）。同样的错误不再重复显示。异常： {ex}", ratkinOrder.LoadID ^ 0x15676231);
                }
            }
        }
    }

    public bool FactionHasRatkinOrder(Faction faction)
    {
        if (faction is null)
        {
            return false;
        }

        foreach (RatkinOrder ratkinOrder in allRatkinOrders)
        {
            if (ratkinOrder.Faction == faction)
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

        foreach (RatkinOrder ratkinOrder in allRatkinOrders)
        {
            if (ratkinOrder.Faction == faction)
            {
                return ratkinOrder;
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

        ResidentPawnsManager.Instance.Notify_RatkinOrderRemoved(order);
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
