using RimWorld;
using RimWorld.QuestGen;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_OrderEsteemChange : QuestNode
{
    public SlateRef<string> inSignal;

    public SlateRef<RatkinOrder> ratkinOrder;
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

        QuestPart_OrderEsteemChange questPart_OrderEsteemChange = new()
        {
            InSignalTrigger = inSignal.GetValue(slate) ?? slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal),
            RatkinOrder = ratkinOrder.GetValue(slate) ?? slate.Get<RatkinOrder>(KeyLibrary_SlateStoreAs.ratkinOrder),
            Change = change.GetValue(slate),
            ShowPlayerChangeMessage = showPlayerChangeMessage.GetValue(slate),
            Reason = reason.GetValue(slate)
        };

        QuestGen.quest.AddPart(questPart_OrderEsteemChange);

        if (isReward.GetValue(slate))
        {
            QuestPart_Choice questPart_Choice;
            Reward_OrderEsteem reward = new()
            {
                RatkinOrder = questPart_OrderEsteemChange.RatkinOrder,
                Amount = questPart_OrderEsteemChange.Change,
                Reason = questPart_OrderEsteemChange.Reason
            };

            if (isSingleReward.GetValue(slate))
            {
                questPart_Choice = new QuestPart_Choice()
                {
                    inSignalChoiceUsed = questPart_OrderEsteemChange.InSignalTrigger,
                };

                questPart_Choice.choices.Add(new QuestPart_Choice.Choice() { rewards = [reward] });
                QuestGen.quest.AddPart(questPart_Choice);
            }
            else
            {
                questPart_Choice = QuestGen.quest.PartsListForReading.OfType<QuestPart_Choice>().FirstOrFallback(null);
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

public class QuestPart_OrderEsteemChange : QuestPart, IOnRatkinOrderRemoved
{
    public string InSignalTrigger;
    public RatkinOrder RatkinOrder;
    public int Change;
    public bool ShowPlayerChangeMessage = true;
    public string Reason;
    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (RatkinOrder.IsValid() && signal.tag == InSignalTrigger)
        {
            RatkinOrder.EsteemHandler.AdjustEsteem(Change, byPlayer: true, reason: Reason);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        InSignalTrigger = null;
        RatkinOrder = null;
        Reason = null;
        Change = 0;
        ShowPlayerChangeMessage = true;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref InSignalTrigger, nameof(InSignalTrigger));
        Scribe_References.Look(ref RatkinOrder, nameof(RatkinOrder));
        Scribe_Values.Look(ref Change, nameof(Change), 0);
        Scribe_Values.Look(ref ShowPlayerChangeMessage, nameof(ShowPlayerChangeMessage), defaultValue: true);
        Scribe_Values.Look(ref Reason, nameof(Reason));
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (RatkinOrder == ratkinOrder)
        {
            RatkinOrder = null;
        }
    }
}
