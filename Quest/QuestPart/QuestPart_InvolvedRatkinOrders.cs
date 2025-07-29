using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_InvolvedRatkinOrders : QuestPart, IRatkinOrderRelated
{
    public List<RatkinOrder> orders = [];

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref orders, "orders", LookMode.Reference);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        orders = null;
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        orders?.Remove(order);
    }

    public static void AddInvolvedRatkinOrder(Quest quest, RatkinOrder order)
    {
        QuestPart_InvolvedRatkinOrders questPart_InvolvedRatkinOrders = quest.PartsListForReading.OfType<QuestPart_InvolvedRatkinOrders>().FirstOrFallback(null);
        if (questPart_InvolvedRatkinOrders is null)
        {
            questPart_InvolvedRatkinOrders = new QuestPart_InvolvedRatkinOrders();
            questPart_InvolvedRatkinOrders.orders.Add(order);
            quest.AddPart(questPart_InvolvedRatkinOrders);
        }
        else
        {
            questPart_InvolvedRatkinOrders.orders.AddDistinct(order);
        }
    }

    public static void AddInvolvedRatkinOrder(Quest quest, IEnumerable<RatkinOrder> orders)
    {
        QuestPart_InvolvedRatkinOrders questPart_InvolvedRatkinOrders = quest.PartsListForReading.OfType<QuestPart_InvolvedRatkinOrders>().FirstOrFallback(null);
        if (questPart_InvolvedRatkinOrders is null)
        {
            questPart_InvolvedRatkinOrders = new QuestPart_InvolvedRatkinOrders();
            foreach (RatkinOrder order in orders)
            {
                questPart_InvolvedRatkinOrders.orders.AddDistinct(order);
            }
            quest.AddPart(questPart_InvolvedRatkinOrders);
        }
        else
        {
            foreach (RatkinOrder order in orders)
            {
                questPart_InvolvedRatkinOrders.orders.AddDistinct(order);
            }
        }
    }
}