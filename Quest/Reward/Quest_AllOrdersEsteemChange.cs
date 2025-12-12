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
            InSignalTrigger = inSignal.GetValue(slate) ?? slate.Get<string>("inSignal"),
            Change = change.GetValue(slate),
            ShowPlayerChangeMessage = showPlayerChangeMessage.GetValue(slate),
            Reason = reason.GetValue(slate)
        };

        quest.AddPart(questPart_AllOrdersEsteemChange);

        if (isReward.GetValue(slate))
        {
            QuestPart_Choice questPart_Choice;
            Reward_AllOrdersEsteem reward = new()
            {
                Amount = change.GetValue(slate),
                Reason = reason.GetValue(slate)
            };

            if (isSingleReward.GetValue(slate))
            {
                questPart_Choice = new QuestPart_Choice()
                {
                    inSignalChoiceUsed = questPart_AllOrdersEsteemChange.InSignalTrigger,
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
    public string InSignalTrigger;
    public int Change;
    public bool ShowPlayerChangeMessage = true;
    public string Reason;

    public QuestPart_AllOrdersEsteemChange() { }
    public QuestPart_AllOrdersEsteemChange(string inSignalTrigger, int change, bool showPlayerChangeMessage = true, string reason = null)
    {
        InSignalTrigger = inSignalTrigger;
        Change = change;
        ShowPlayerChangeMessage = showPlayerChangeMessage;
        Reason = reason;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (signal.tag == InSignalTrigger)
        {
            EsteemUtility.AdjustAllOrdersEsteem(Change, byPlayer: true, ShowPlayerChangeMessage, Reason);
        }
    }
    public override void Cleanup()
    {
        base.Cleanup();
        InSignalTrigger = null;
        Change = 0;
        ShowPlayerChangeMessage = true;
        Reason = null;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref InSignalTrigger, "InSignalTrigger");
        Scribe_Values.Look(ref Change, "Change", 0);
        Scribe_Values.Look(ref ShowPlayerChangeMessage, "ShowPlayerChangeMessage", defaultValue: true);
        Scribe_Values.Look(ref Reason, KeyLibrary_FormatArgName.Reason);
    }
}
