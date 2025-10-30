using RimWorld;
using RimWorld.QuestGen;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_GiveBranchMedal : QuestNode
{
    public SlateRef<string> inSignal;

    public SlateRef<Branch> branch;
    public SlateRef<BranchMedalRecord.BranchMedalType?> potentialTypes;
    public SlateRef<short> count;

    public SlateRef<bool> isReward;
    public SlateRef<bool> isSingleReward;

    protected override bool TestRunInt(Slate slate)
    {
        return potentialTypes.GetValue(slate).HasValue;
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
            InSignalTrigger = inSignal.GetValue(slate) ?? slate.Get<string>("inSignal"),
            Branch = branch.GetValue(slate) ?? slate.Get<Branch>(KeyLibrary_SlateStoreAs.Branch),
            Count = count.GetValue(slate),
            PotentialTypes = potentialTypes.GetValue(slate).GetValueOrDefault(defaultValue: BranchMedalRecord.BranchMedalType.None),
        };

        QuestGen.quest.AddPart(questPart_GiveBranchMedal);

        if (isReward.GetValue(slate))
        {
            QuestPart_Choice questPart_Choice;
            Reward_BranchMedals reward = new()
            {
                Branch = questPart_GiveBranchMedal.Branch,
                PotentialTypes = questPart_GiveBranchMedal.PotentialTypes,
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
    public BranchMedalRecord.BranchMedalType PotentialTypes;
    public short Count;

    public override void Cleanup()
    {
        base.Cleanup();
        InSignalTrigger = null;
        Branch = null;
        PotentialTypes = BranchMedalRecord.BranchMedalType.None;
        Count = 0;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref InSignalTrigger, "InSignalTrigger");
        Scribe_References.Look(ref Branch, "Branch");
        Scribe_Values.Look(ref PotentialTypes, "PotentialTypes", BranchMedalRecord.BranchMedalType.None);
        Scribe_Values.Look(ref Count, "Count", (short)0);
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (Count > 0 && Branch is not null && signal.tag == InSignalTrigger)
        {
            BranchMedalRecord.BranchMedalType medalType = BranchUtility.GetContainedBranchMedals(PotentialTypes).RandomElementWithFallback(BranchMedalRecord.BranchMedalType.None);
            if (medalType != BranchMedalRecord.BranchMedalType.None)
            {
                Branch.MedalHandler.AddMedal(medalType, Count);
            }
        }
    }
}