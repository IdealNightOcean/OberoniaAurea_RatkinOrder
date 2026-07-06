using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_GiveBranchMedal : QuestNode
{
    public SlateRef<string> inSignal;

    public SlateRef<Branch> branch;
    public SlateRef<IEnumerable<KnightChivalryDef>> potentialDefs;
    public SlateRef<int> count;

    public SlateRef<bool> isReward;
    public SlateRef<bool> isSingleReward;

    protected override bool TestRunInt(Slate slate)
    {
        return potentialDefs.GetValue(slate) is not null;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        if (count.GetValue(slate) <= 0)
        {
            return;
        }
        QuestPart_GiveBranchMedal questPart_GiveBranchMedal = new()
        {
            InSignalTrigger = inSignal.GetValue(slate) ?? slate.Get<string>(OARO_KeyLibrary_SlateStoreAs.inSignal),
            Branch = branch.GetValue(slate) ?? slate.Get<Branch>(OARO_KeyLibrary_SlateStoreAs.branch),
            Count = count.GetValue(slate),
            PotentialDefs = [],
        };
        IEnumerable<KnightChivalryDef> potentialDefs = this.potentialDefs.GetValue(slate);
        if (potentialDefs is not null)
        {
            questPart_GiveBranchMedal.PotentialDefs.AddRangeUnique(potentialDefs);
        }

        QuestGen.quest.AddPart(questPart_GiveBranchMedal);

        if (isReward.GetValue(slate))
        {
            QuestPart_Choice questPart_Choice;
            Reward_BranchMedals reward = new()
            {
                Branch = questPart_GiveBranchMedal.Branch,
                PotentialDefs = [.. questPart_GiveBranchMedal.PotentialDefs],
                Amount = questPart_GiveBranchMedal.Count
            };

            if (isSingleReward.GetValue(slate))
            {
                questPart_Choice = new QuestPart_Choice()
                {
                    inSignalChoiceUsed = questPart_GiveBranchMedal.InSignalTrigger,
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


public class QuestPart_GiveBranchMedal : QuestPart
{
    public string InSignalTrigger;
    public Branch Branch;
    public List<KnightChivalryDef> PotentialDefs;
    public int Count;

    public override void Cleanup()
    {
        base.Cleanup();
        InSignalTrigger = null;
        Branch = null;
        PotentialDefs = null;
        Count = 0;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref InSignalTrigger, nameof(InSignalTrigger));
        Scribe_References.Look(ref Branch, nameof(Branch));
        Scribe_Collections.Look(ref PotentialDefs, nameof(PotentialDefs), LookMode.Def);
        Scribe_Values.Look(ref Count, nameof(Count), 0);
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (Count > 0 && Branch.IsValid() && signal.tag == InSignalTrigger)
        {
            if (PotentialDefs.NullOrEmpty())
                return;

            PotentialDefs.RemoveAll(c => c.medal is null);
            KnightChivalryDef medalChivalry = PotentialDefs.RandomElementWithFallback(null);
            if (medalChivalry is not null)
            {
                Branch.MedalHandler.AdjustMedal(medalChivalry, Count);
            }
        }
    }
}