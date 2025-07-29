using RimWorld;
using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_GetRatkinOrderOfFaction : QuestNode
{

    public SlateRef<string> storeAs;
    public SlateRef<Faction> faction;

    protected override bool TestRunInt(Slate slate)
    {
        return faction.GetValue(slate) is not null && RatkinOrderManager.Instance.IsFactionHasRatkinOrder(faction.GetValue(slate));
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        RatkinOrder ratkinOrder = RatkinOrderManager.Instance.GetRatkinOrderForFaction(faction.GetValue(slate));
        if (ratkinOrder is not null)
        {
            slate.Set(storeAs.GetValue(slate) ?? ModUtility.RatkinOrderStoreAs, ratkinOrder);
            QuestPart_InvolvedRatkinOrders.AddInvolvedRatkinOrder(QuestGen.quest, ratkinOrder);
        }
    }
}
