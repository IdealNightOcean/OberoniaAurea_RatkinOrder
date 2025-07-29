using RimWorld;
using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public abstract class QuestNode_GetRatkinOrderBase : QuestNode
{
    public SlateRef<string> storeAs = KeyLibrary_SlateStoreAs.RatkinOrderStoreAs;

    public SlateRef<bool> isCritical;
    public SlateRef<bool> endQuestWhenOrderInvalid;
    public SlateRef<QuestEndOutcome> questEndOutcome = QuestEndOutcome.Unknown;

    protected abstract RatkinOrder GetRatkinOrder(Slate slate);

    protected override bool TestRunInt(Slate slate)
    {
        RatkinOrder order = GetRatkinOrder(slate);
        if (order is null)
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

        if (ratkinOrder is null)
        {
            return;
        }

        Quest quest = QuestGen.quest;

        slate.Set(storeAs.GetValue(slate), ratkinOrder);
        if (isCritical.GetValue(slate))
        {
            QuestPart_CriticalRatkinOrder questPart_CriticalRatkinOrder = new()
            {
                order = ratkinOrder,
                endQuest = endQuestWhenOrderInvalid.GetValue(slate),
                endOutcome = questEndOutcome.GetValue(slate)
            };
            quest.AddPart(questPart_CriticalRatkinOrder);
        }

        quest.AddInvolvedFaction(ratkinOrder.Faction);
        QuestPart_InvolvedRatkinOrders.AddInvolvedRatkinOrder(quest, ratkinOrder);
    }
}
