using RimWorld;
using RimWorld.QuestGen;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_OrderEsteemChange : QuestNode
{
    public SlateRef<string> inSignal;

    public SlateRef<RatkinOrder> order;
    public SlateRef<int> change;
    public SlateRef<string> reason;
    public SlateRef<bool> showPlayerChangeMessage = true;

    public SlateRef<bool> isReward;
    public SlateRef<bool> isSingleReward;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        if (change.GetValue(slate) == 0)
        {
            return;
        }

        Quest quest = QuestGen.quest;

        QuestPart_OrderEsteemChange questPart_OrderEsteemChange = new()
        {
            inSignalTrigger = inSignal.GetValue(slate) ?? slate.Get<string>("inSignal"),
            order = order.GetValue(slate) ?? slate.Get<RatkinOrder>(KeyLibrary_SlateStoreAs.RatkinOrderStoreAs),
            change = change.GetValue(slate),
            showPlayerChangeMessage = showPlayerChangeMessage.GetValue(slate),
            reason = reason.GetValue(slate)
        };

        quest.AddPart(questPart_OrderEsteemChange);

        if (isReward.GetValue(slate))
        {
            QuestPart_Choice questPart_Choice;
            Reward_OrderEsteem reward = new()
            {
                order = order.GetValue(slate),
                amount = change.GetValue(slate),
                reason = reason.GetValue(slate)
            };

            if (isSingleReward.GetValue(slate))
            {
                questPart_Choice = new QuestPart_Choice()
                {
                    inSignalChoiceUsed = questPart_OrderEsteemChange.inSignalTrigger,
                };

                questPart_Choice.choices.Add(new QuestPart_Choice.Choice() { rewards = [reward] });
                quest.AddPart(questPart_Choice);
            }
            else
            {
                questPart_Choice = quest.PartsListForReading.OfType<QuestPart_Choice>().FirstOrFallback(null);
                if (questPart_Choice is not null)
                {
                    foreach (QuestPart_Choice.Choice singelChoice in questPart_Choice.choices)
                    {
                        singelChoice.rewards.Add(reward);
                    }
                }
            }
        }
    }
}

public class QuestPart_OrderEsteemChange : QuestPart
{
    public string inSignalTrigger;
    public RatkinOrder order;
    public int change;
    public bool showPlayerChangeMessage = true;
    public string reason;
    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (signal.tag == inSignalTrigger)
        {
            order?.EsteemHandler.AdjustEsteem(change, byPlayer: true, reason: reason);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        inSignalTrigger = string.Empty;
        order = null;
        reason = null;
        change = 0;
        showPlayerChangeMessage = true;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignalTrigger, "inSignalTrigger");
        Scribe_References.Look(ref order, "order");
        Scribe_Values.Look(ref change, "change", 0);
        Scribe_Values.Look(ref showPlayerChangeMessage, "showPlayerChangeMessage", defaultValue: true);
        Scribe_Values.Look(ref reason, "reason");
    }
}
