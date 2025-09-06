using RimWorld;
using RimWorld.QuestGen;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_SetBranchToFriendly : QuestNode
{
    public SlateRef<string> inSignal;

    public SlateRef<Branch> branch;
    public SlateRef<int> durationDays = Reward_FriendlyBranch.DefaultFriendlyDays;
    public SlateRef<bool> showMessage = true;

    public SlateRef<bool> isReward;
    public SlateRef<bool> isSingleReward;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        if (durationDays.GetValue(slate) == 0)
        {
            return;
        }

        Quest quest = QuestGen.quest;

        QuestPart_SetBranchToFriendly questPart_SetBranchToFriendly = new()
        {
            InSignalTrigger = inSignal.GetValue(slate) ?? slate.Get<string>("inSignal"),
            Branch = branch.GetValue(slate) ?? slate.Get<Branch>(KeyLibrary_SlateStoreAs.Branch),
            DurationDays = durationDays.GetValue(slate),
            ShowMessage = showMessage.GetValue(slate),
        };

        quest.AddPart(questPart_SetBranchToFriendly);

        if (isReward.GetValue(slate))
        {
            QuestPart_Choice questPart_Choice;
            Reward_FriendlyBranch reward = new()
            {
                Branch = questPart_SetBranchToFriendly.Branch,
                DurationDays = questPart_SetBranchToFriendly.DurationDays
            };

            if (isSingleReward.GetValue(slate))
            {
                questPart_Choice = new QuestPart_Choice()
                {
                    inSignalChoiceUsed = questPart_SetBranchToFriendly.InSignalTrigger,
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

public class QuestPart_SetBranchToFriendly : QuestPart, IOnBranchDestoryed
{
    public string InSignalTrigger;
    public Branch Branch;
    public int DurationDays = Reward_FriendlyBranch.DefaultFriendlyDays;
    public bool ShowMessage = true;
    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (signal.tag == InSignalTrigger)
        {
            Branch?.SetFriendly(friendly: true, durationTick: DurationDays * 60000, showMessage: ShowMessage);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        InSignalTrigger = string.Empty;
        Branch = null;
        DurationDays = 0;
        ShowMessage = true;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref InSignalTrigger, "InSignalTrigger");
        Scribe_References.Look(ref Branch, "Branch");
        Scribe_Values.Look(ref DurationDays, "DurationDays", Reward_FriendlyBranch.DefaultFriendlyDays);
        Scribe_Values.Look(ref ShowMessage, "ShowMessage", defaultValue: true);
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (Branch?.RatkinOrder == ratkinOrder)
        {
            Branch = null;
        }
    }
    public void Notify_BranchDestoryed(Branch branch)
    {
        if (Branch == branch)
        {
            Branch = null;
        }
    }
}
