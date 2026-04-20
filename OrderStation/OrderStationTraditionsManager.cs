using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士驻地传统工作器
/// </summary>
public class OrderStationTraditionWorker
{
    public OrderStationTraditionDef Def { get; private set; }

    public static OrderStationTraditionWorker CreateWorker(OrderStationTraditionDef def)
    {
        OrderStationTraditionWorker worker = (OrderStationTraditionWorker)Activator.CreateInstance(def.workerClass);
        worker.Def = def;
        return worker;
    }

    public virtual bool ShouldActiveNow()
    {
        return HasRequiredChivalryKnights();
    }

    public virtual void PostActive() { }

    public virtual void PostDeactive() { }

    protected bool HasRequiredChivalryKnights()
    {
        if (Def.Chivalry is null)
            return true;
        Dictionary<KnightChivalryDef, int> knightsWithChivalryCount = ResidentPawnsManager.CacheManager?.KnightsWithChivalryCount.Value;
        if (knightsWithChivalryCount is null)
            return false;
        if (knightsWithChivalryCount.TryGetValue(Def.Chivalry, out int count))
        {
            return count >= Def.requiredKnightCount;
        }
        return false;
    }
}

/// <summary>
/// 骑士驻地传统管理器
/// </summary>
public class OrderStationTraditionsManager : IExposable
{
    public HashSet<OrderStationTraditionDef> activeTraditions = [];
    private Dictionary<KnightChivalryDef, int> activeTraditionsWithChivalryCountCache = [];
    private bool TraditionsChanged { get; set; } = false;

    public int ActiveTraditionCount => activeTraditions.Count;

    public void ExposeData()
    {
        Scribe_Collections.Look(ref activeTraditions, nameof(activeTraditions), LookMode.Def);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (activeTraditions.Remove(null))
            {
                Log.Error("[OARO] 部分驻地传统在加载后为 null，已移除");
            }
        }
    }

    public bool HasTradition(OrderStationTraditionDef traditionDef) => activeTraditions.Contains(traditionDef);

    public int GetTraditionCountOfChivalry(KnightChivalryDef chivalry)
    {
        int count = 0;
        foreach (OrderStationTraditionDef tradition in activeTraditions)
        {
            if (chivalry.IsSameDefNonNullable(tradition.Chivalry))
                count++;
        }
        return count;
    }

    public float GetAcademicCostReduction(KnightChivalryDef chivalry)
    {
        float reduction = 0f;
        foreach (OrderStationTraditionDef tradition in activeTraditions)
        {
            if (chivalry.IsSameDefNonNullable(tradition.Chivalry))
                reduction += tradition.academicCostReduction;
        }
        return reduction;
    }

    public void TickDay()
    {
        foreach (OrderStationTraditionDef tradition in DefDatabase<OrderStationTraditionDef>.AllDefs)
        {
            bool shouldActive = tradition.Worker.ShouldActiveNow();
            bool isActive = activeTraditions.Contains(tradition);

            if (shouldActive && !isActive)
            {
                activeTraditions.Add(tradition);
                tradition.Worker.PostActive();
                SendTraditionActivatedLetter(tradition);
                TraditionsChanged = true;
            }
            else if (!shouldActive && isActive)
            {
                activeTraditions.Remove(tradition);
                tradition.Worker.PostDeactive();
                SendTraditionDeactivatedLetter(tradition);
                TraditionsChanged = true;
            }
        }

        if (TraditionsChanged)
        {
            ReapplyTraditionEffects();
        }
    }

    private void ReapplyTraditionEffects()
    {
        TraditionsChanged = false;
    }

    private static void SendTraditionActivatedLetter(OrderStationTraditionDef traditionDef)
    {
        Find.LetterStack.ReceiveLetter(
            label: "OARO_LetterLabel_TraditionEstablished".Translate(traditionDef.Named(KeyLibrary_FormatArgName.TRADITIONDEF)),
            text: "OARO_LetterText_TraditionEstablished".Translate(traditionDef.Named(KeyLibrary_FormatArgName.TRADITIONDEF)),
            textLetterDef: LetterDefOf.PositiveEvent);
    }

    private static void SendTraditionDeactivatedLetter(OrderStationTraditionDef traditionDef)
    {
        Find.LetterStack.ReceiveLetter(
            label: "OARO_LetterLabel_TraditionLost".Translate(traditionDef.Named(KeyLibrary_FormatArgName.TRADITIONDEF)),
            text: "OARO_LetterText_TraditionLost".Translate(traditionDef.Named(KeyLibrary_FormatArgName.TRADITIONDEF)),
            textLetterDef: LetterDefOf.NegativeEvent);
    }
}
