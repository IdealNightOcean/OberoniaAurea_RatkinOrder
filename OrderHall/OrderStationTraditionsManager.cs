using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;


public class OrderStationTraditionDef : Def
{
    public Type workerClass = typeof(OrderStationTraditionWorker);

    private OrderStationTraditionWorker worker;
    public OrderStationTraditionWorker Worker => worker ??= OrderStationTraditionWorker.CreateWorker(this);

    public KnightVirtueDef relatedVirtue;
}

public class OrderStationTraditionWorker
{
    public OrderStationTraditionDef Def { get; private set; }

    public virtual bool ShouldActiveNow() => true;

    public virtual void PostActive() { }
    public virtual void PostDeactive() { }

    public static OrderStationTraditionWorker CreateWorker(OrderStationTraditionDef def)
    {
        OrderStationTraditionWorker worker = (OrderStationTraditionWorker)Activator.CreateInstance(def.workerClass);
        worker.Def = def;
        return worker;
    }
}

public class OrderStationTraditionsManager : IExposable
{
    public HashSet<OrderStationTraditionDef> activeTraditions = [];
    private bool traditionsChanged = false;

    public void TickDay()
    {
        foreach (OrderStationTraditionDef tradition in DefDatabase<OrderStationTraditionDef>.AllDefs)
        {
            if (tradition.Worker.ShouldActiveNow())
            {
                if (!activeTraditions.Contains(tradition))
                {
                    activeTraditions.Add(tradition);
                    tradition.Worker.PostActive();
                    traditionsChanged = true;
                }
            }
            else
            {
                if (activeTraditions.Contains(tradition))
                {
                    activeTraditions.Remove(tradition);
                    tradition.Worker.PostDeactive();
                    traditionsChanged = true;
                }
            }
        }

        if (traditionsChanged)
        {
            ReapplyTraditionEffects();
        }
    }

    public void ReapplyTraditionEffects()
    {
        traditionsChanged = false;
        throw new NotImplementedException();

    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref activeTraditions, nameof(activeTraditions), LookMode.Def);
    }
}
