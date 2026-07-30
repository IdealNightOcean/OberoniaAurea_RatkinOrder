using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_GiveBranchMedal_CriticalDemand : QuestNode
{
    public SlateRef<string> inSignal;

    public SlateRef<Branch> branch;
    public SlateRef<IEnumerable<KnightChivalryDef>> potentialDefs;
    public SlateRef<int> count;

    public SlateRef<int> baseRewardMedalTypeCount = 2;
    public SlateRef<int> extraRewardMedalTypeCount = 1;
    public SlateRef<float> extraMedalPotencyBoundary;

    public SlateRef<bool> isReward;
    public SlateRef<bool> isSingleReward;

    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        if (count.GetValue(slate) <= 0)
        {
            return;
        }
        QuestPart_GiveBranchMedal_CriticalDemand questPart_GiveBranchMedal_CriticalDemand = new()
        {
            InSignalTrigger = inSignal.GetValue(slate) ?? slate.Get<string>(OARO_KeyLibrary_SlateStoreAs.inSignal),
            Branch = branch.GetValue(slate) ?? slate.Get<Branch>(OARO_KeyLibrary_SlateStoreAs.branch),
            Count = count.GetValue(slate),
            BaseRewardMedalTypeCount = baseRewardMedalTypeCount.GetValue(slate),
            ExtraRewardMedalTypeCount = extraRewardMedalTypeCount.GetValue(slate),
            ExtraMedalPotencyBoundary = extraMedalPotencyBoundary.GetValue(slate),
            PotentialMedalDefs = [],
        };
        IEnumerable<KnightChivalryDef> potentialDefs = this.potentialDefs.GetValue(slate) ?? slate.Get<IEnumerable<KnightChivalryDef>>(OARO_KeyLibrary_SlateStoreAs.preSetPotentialMedals);
        if (potentialDefs is not null)
        {
            questPart_GiveBranchMedal_CriticalDemand.PotentialMedalDefs.AddRangeUnique(potentialDefs);
        }

        QuestGen.quest.AddPart(questPart_GiveBranchMedal_CriticalDemand);

        if (isReward.GetValue(slate))
        {
            QuestPart_Choice questPart_Choice;
            Reward_BranchMedals reward = new()
            {
                Branch = questPart_GiveBranchMedal_CriticalDemand.Branch,
                PotentialDefs = [.. questPart_GiveBranchMedal_CriticalDemand.PotentialMedalDefs],
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
    public List<KnightChivalryDef> PotentialMedalDefs;
    public int Count;

    public int BaseRewardMedalTypeCount;
    public int ExtraRewardMedalTypeCount;

    public float ExtraMedalPotencyBoundary;

    public override void Cleanup()
    {
        base.Cleanup();
        InSignalTrigger = null;
        Branch = null;
        PotentialMedalDefs = null;
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
        Scribe_Collections.Look(ref PotentialMedalDefs, nameof(PotentialMedalDefs), LookMode.Def);
        Scribe_Values.Look(ref Count, nameof(Count), 0);
        Scribe_Values.Look(ref BaseRewardMedalTypeCount, nameof(BaseRewardMedalTypeCount), 0);
        Scribe_Values.Look(ref ExtraRewardMedalTypeCount, nameof(ExtraRewardMedalTypeCount), 0);
        Scribe_Values.Look(ref ExtraMedalPotencyBoundary, nameof(ExtraMedalPotencyBoundary), 0f);
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (!Branch.IsValid() || signal.tag != InSignalTrigger)
        {
            return;
        }

        if (!quest.TryGetCliquesManager(addPartIfMiss: false, out QuestPart_CliquesManager cliquesManager))
        {
            return;
        }

        StringBuilder medalRewardSB = new("OARO_CriticalDemand_CliqueBranchMedalGainText".Translate(
            Branch.RatkinOrder.NameColored.Named(OARO_KeyLibrary_FormatArgName.OrderName),
            Branch.NameColored.Named(OARO_KeyLibrary_FormatArgName.BranchName),
            quest.name.Named("QuestName")
            ));

        medalRewardSB.AppendLine();
        cliquesManager.TotalPotency.MarkDirty();
        float totalPotency = cliquesManager.TotalPotency.Value;

        Dictionary<KnightChivalryDef, int> gainMedals = [];
        foreach (QuestClique clique in cliquesManager.AllCliques)
        {
            if (!clique.IsBranchClique)
            {
                continue;
            }

            Branch branch = clique.RelatedBranch;
            BranchMedalHandler medalHandler = branch.MedalHandler;

            KnightChivalryDef medalChivalry;
            int gainMedalCount;

            if (clique.FocusedTaskChivalry?.medal is not null)
            {
                gainMedalCount = 1;
                medalHandler.AdjustMedal(clique.FocusedTaskChivalry, gainMedalCount);
                gainMedals[clique.FocusedTaskChivalry] = gainMedals.TryGetValue(clique.FocusedTaskChivalry, fallback: 0) + gainMedalCount;
            }

            if (!PotentialMedalDefs.NullOrEmpty())
            {
                if (branch == Branch)
                {
                    gainMedalCount = Mathf.CeilToInt(totalPotency / 0.5f);
                    if (gainMedalCount > 0)
                    {
                        medalChivalry = PotentialMedalDefs.RandomElement();
                        medalHandler.AdjustMedal(medalChivalry, gainMedalCount);
                        gainMedals[medalChivalry] = gainMedals.TryGetValue(medalChivalry, fallback: 0) + gainMedalCount;
                    }
                }
                else
                {
                    gainMedalCount = Mathf.CeilToInt(totalPotency / 1f);
                    if (gainMedalCount > 0)
                    {
                        medalChivalry = PotentialMedalDefs.RandomElement();
                        medalHandler.AdjustMedal(medalChivalry, gainMedalCount);
                        gainMedals[medalChivalry] = gainMedals.TryGetValue(medalChivalry, fallback: 0) + gainMedalCount;
                    }
                }
            }

            if (gainMedals.Count > 0)
            {
                medalRewardSB.AppendLine(branch.NameColored);
                foreach (KeyValuePair<KnightChivalryDef, int> kv in gainMedals)
                {
                    medalRewardSB.AppendWithSeparator($"{kv.Key.LabelCap} × {kv.Value}".Colorize(kv.Key.color), ", ");
                }
                gainMedals.Clear();
            }
        }

        OrderLetterUtility.ReceiveLetter(
            label: "OARO_CriticalDemand_CliqueBranchMedalGainLabel".Translate(quest.name.Named("QuestName")).CapitalizeFirst(),
            text: medalRewardSB.ToString(),
            def: OrderLetterDefOf.OARO_OfficialLetter,
            relatedOrder: Branch.RatkinOrder,
            relatedBranch: Branch,
            sender: Branch.NameColored,
            relatedLetterType: OrderLetter.RelatedLetterType.Positive);
    }
}