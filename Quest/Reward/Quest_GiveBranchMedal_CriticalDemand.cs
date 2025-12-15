using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_GiveBranchMedal_CriticalDemand : QuestNode
{
    public SlateRef<string> inSignal;

    public SlateRef<Branch> branch;
    public SlateRef<IEnumerable<BranchMedalDef>> potentialDefs;
    public SlateRef<int> count;

    public SlateRef<int> baseRewardMedalTypeCount = 2;
    public SlateRef<int> extraRewardMedalTypeCount = 1;
    public SlateRef<float> extraMedalPotencyBoundary;

    public SlateRef<bool> isReward;
    public SlateRef<bool> isSingleReward;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        if (count.GetValue(slate) <= 0)
        {
            return;
        }
        QuestPart_GiveBranchMedal_CriticalDemand questPart_GiveBranchMedal_CriticalDemand = new()
        {
            InSignalTrigger = inSignal.GetValue(slate) ?? slate.Get<string>("inSignal"),
            Branch = branch.GetValue(slate) ?? slate.Get<Branch>(KeyLibrary_SlateStoreAs.Branch),
            Count = count.GetValue(slate),
            BaseRewardMedalTypeCount = baseRewardMedalTypeCount.GetValue(slate),
            ExtraRewardMedalTypeCount = extraRewardMedalTypeCount.GetValue(slate),
            ExtraMedalPotencyBoundary = extraMedalPotencyBoundary.GetValue(slate),
            PotentialDefs = [],
        };
        IEnumerable<BranchMedalDef> potentialDefs = this.potentialDefs.GetValue(slate) ?? slate.Get<IEnumerable<BranchMedalDef>>(KeyLibrary_SlateStoreAs.PreSetPotentialMedals);
        if (potentialDefs is not null)
        {
            questPart_GiveBranchMedal_CriticalDemand.PotentialDefs.AddRangeUnique(potentialDefs);
        }

        QuestGen.quest.AddPart(questPart_GiveBranchMedal_CriticalDemand);

        if (isReward.GetValue(slate))
        {
            QuestPart_Choice questPart_Choice;
            Reward_BranchMedals reward = new()
            {
                Branch = questPart_GiveBranchMedal_CriticalDemand.Branch,
                PotentialDefs = [.. questPart_GiveBranchMedal_CriticalDemand.PotentialDefs],
                Amount = questPart_GiveBranchMedal_CriticalDemand.Count
            };

            if (isSingleReward.GetValue(slate))
            {
                questPart_Choice = new QuestPart_Choice()
                {
                    inSignalChoiceUsed = questPart_GiveBranchMedal_CriticalDemand.InSignalTrigger,
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

public class QuestPart_GiveBranchMedal_CriticalDemand : QuestPart
{
    public string InSignalTrigger;
    public Branch Branch;
    public List<BranchMedalDef> PotentialDefs;
    public int Count;

    public int BaseRewardMedalTypeCount;
    public int ExtraRewardMedalTypeCount;

    public float ExtraMedalPotencyBoundary;

    public override void Cleanup()
    {
        base.Cleanup();
        InSignalTrigger = null;
        Branch = null;
        PotentialDefs = null;
        Count = 0;
        BaseRewardMedalTypeCount = 0;
        ExtraRewardMedalTypeCount = 0;
        ExtraMedalPotencyBoundary = 0f;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref InSignalTrigger, nameof(InSignalTrigger));
        Scribe_References.Look(ref Branch, nameof(Branch));
        Scribe_Collections.Look(ref PotentialDefs, nameof(PotentialDefs), LookMode.Def);
        Scribe_Values.Look(ref Count, nameof(Count), 0);
        Scribe_Values.Look(ref BaseRewardMedalTypeCount, nameof(BaseRewardMedalTypeCount), 0);
        Scribe_Values.Look(ref ExtraRewardMedalTypeCount, nameof(ExtraRewardMedalTypeCount), 0);
        Scribe_Values.Look(ref ExtraMedalPotencyBoundary, nameof(ExtraMedalPotencyBoundary), 0f);
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (Count > 0 && Branch.IsValid() && PotentialDefs is not null && signal.tag == InSignalTrigger)
        {
            int rewardMedalTypeCount = BaseRewardMedalTypeCount;

            if (quest.TryGetCliquesManager(addPartIfMiss: false, out QuestPart_CliquesManager cliquesManager) && cliquesManager.TotalPotency.Value >= ExtraMedalPotencyBoundary)
            {
                rewardMedalTypeCount += ExtraRewardMedalTypeCount;
            }

            rewardMedalTypeCount = rewardMedalTypeCount > 0 ? rewardMedalTypeCount : 1;
            foreach (BranchMedalDef medalType in PotentialDefs.TakeRandom(rewardMedalTypeCount))
            {
                Branch.MedalHandler.AddMedal(medalType, Count);
            }
        }
    }
}