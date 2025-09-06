using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_InvolvedRatkinOrders : QuestPart, IOnRatkinOrderRemoved
{
    public List<RatkinOrder> RatkinOrders = [];

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref RatkinOrders, "RatkinOrders", LookMode.Reference);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        RatkinOrders = null;
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        RatkinOrders?.Remove(order);
    }

    public static void AddInvolvedRatkinOrder(Quest quest, RatkinOrder order)
    {
        QuestPart_InvolvedRatkinOrders questPart_InvolvedRatkinOrders = quest.PartsListForReading?.OfType<QuestPart_InvolvedRatkinOrders>()?.FirstOrFallback(null);
        if (questPart_InvolvedRatkinOrders is null)
        {
            questPart_InvolvedRatkinOrders = new QuestPart_InvolvedRatkinOrders();
            questPart_InvolvedRatkinOrders.RatkinOrders.Add(order);
            quest.AddPart(questPart_InvolvedRatkinOrders);
        }
        else
        {
            questPart_InvolvedRatkinOrders.RatkinOrders.AddDistinct(order);
        }
    }

    public static void AddInvolvedRatkinOrder(Quest quest, IEnumerable<RatkinOrder> orders)
    {
        QuestPart_InvolvedRatkinOrders questPart_InvolvedRatkinOrders = quest.PartsListForReading.OfType<QuestPart_InvolvedRatkinOrders>()?.FirstOrFallback(null);
        if (questPart_InvolvedRatkinOrders is null)
        {
            questPart_InvolvedRatkinOrders = new QuestPart_InvolvedRatkinOrders();
            foreach (RatkinOrder order in orders)
            {
                questPart_InvolvedRatkinOrders.RatkinOrders.AddDistinct(order);
            }
            quest.AddPart(questPart_InvolvedRatkinOrders);
        }
        else
        {
            foreach (RatkinOrder order in orders)
            {
                questPart_InvolvedRatkinOrders.RatkinOrders.AddDistinct(order);
            }
        }
    }
}