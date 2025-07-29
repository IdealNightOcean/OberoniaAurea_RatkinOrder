using RimWorld;
using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_SetRatkinOrder : QuestNode
{
    public SlateRef<string> storeAs;
    public SlateRef<RatkinOrder> order;

    protected override bool TestRunInt(Slate slate)
    {
        return order.GetValue(slate) is not null;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        RatkinOrder ratkinOrder = order.GetValue(slate);
        if (ratkinOrder is not null)
        {
            slate.Set(storeAs.GetValue(slate) ?? ModUtility.RatkinOrderStoreAs, ratkinOrder);

            Quest quest = QuestGen.quest;
            quest.AddInvolvedFaction(ratkinOrder.Faction);
            QuestPart_InvolvedRatkinOrders.AddInvolvedRatkinOrder(quest, ratkinOrder);
        }
    }
}
