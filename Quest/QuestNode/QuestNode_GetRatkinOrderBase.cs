using RimWorld;
using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public abstract class QuestNode_GetRatkinOrderBase : QuestNode
{
    public SlateRef<string> storeAs = OARO_KeyLibrary_SlateStoreAs.ratkinOrder;

    public SlateRef<bool> isCritical;
    public SlateRef<bool> endQuestWhenOrderInvalid;
    public SlateRef<QuestEndOutcome> questEndOutcome = QuestEndOutcome.Unknown;

    protected abstract RatkinOrder GetRatkinOrder(Slate slate);

    protected override bool TestRunInt(Slate slate)
    {
        RatkinOrder order = GetRatkinOrder(slate);
        if (!order.IsValid())
        {
            return false;
        }
        else
        {
            slate.Set(storeAs.GetValue(slate), order);
            return true;
        }
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;

        RatkinOrder ratkinOrder = GetRatkinOrder(slate);

        if (!ratkinOrder.IsValid())
        {
            return;
        }

        Quest quest = QuestGen.quest;

        slate.Set(storeAs.GetValue(slate), ratkinOrder);
        if (isCritical.GetValue(slate))
        {
            QuestPart_CriticalRatkinOrder questPart_CriticalRatkinOrder = new()
            {
                RatkinOrder = ratkinOrder,
                EndQuest = endQuestWhenOrderInvalid.GetValue(slate),
                EndOutcome = questEndOutcome.GetValue(slate)
            };
            quest.AddPart(questPart_CriticalRatkinOrder);
        }

        quest.AddInvolvedFaction(ratkinOrder.Faction);
        QuestPart_InvolvedRatkinOrders.AddInvolvedRatkinOrder(quest, ratkinOrder);
    }
}
