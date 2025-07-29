using RimWorld;
using RimWorld.QuestGen;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_AllOrdersEsteemChange : QuestNode
{
    public SlateRef<string> inSignal;

    public SlateRef<int> change;
    public SlateRef<bool> showPlayerChangeMessage = true;
    public SlateRef<string> reason;

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

        QuestPart_AllOrdersEsteemChange questPart_AllOrdersEsteemChange = new()
        {
            inSignalTrigger = inSignal.GetValue(slate) ?? slate.Get<string>("inSignal"),
            change = change.GetValue(slate),
            showPlayerChangeMessage = showPlayerChangeMessage.GetValue(slate),
            reason = reason.GetValue(slate)
        };

        quest.AddPart(questPart_AllOrdersEsteemChange);

        if (isReward.GetValue(slate))
        {
            QuestPart_Choice questPart_Choice;
            Reward_AllOrdersEsteem reward = new()
            {
                amount = change.GetValue(slate),
                reason = reason.GetValue(slate)
            };

            if (isSingleReward.GetValue(slate))
            {
                questPart_Choice = new QuestPart_Choice()
                {
                    inSignalChoiceUsed = questPart_AllOrdersEsteemChange.inSignalTrigger,
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

public class QuestPart_AllOrdersEsteemChange : QuestPart
{
    public string inSignalTrigger;
    public int change;
    public bool showPlayerChangeMessage = true;
    public string reason;

    public QuestPart_AllOrdersEsteemChange() { }
    public QuestPart_AllOrdersEsteemChange(string inSignalTrigger, int change, bool showPlayerChangeMessage = true, string reason = null)
    {
        this.inSignalTrigger = inSignalTrigger;
        this.change = change;
        this.showPlayerChangeMessage = showPlayerChangeMessage;
        this.reason = reason;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (signal.tag == inSignalTrigger)
        {
            EsteemUtility.AdjustAllOrdersEsteem(change, byPlayer: true, showPlayerChangeMessage, reason);
        }
    }
    public override void Cleanup()
    {
        base.Cleanup();
        inSignalTrigger = null;
        change = 0;
        showPlayerChangeMessage = true;
        reason = null;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignalTrigger, "inSignalTrigger");
        Scribe_Values.Look(ref change, "change", 0);
        Scribe_Values.Look(ref showPlayerChangeMessage, "showPlayerChangeMessage", defaultValue: true);
        Scribe_Values.Look(ref reason, "reason");
    }
}
