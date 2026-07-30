using OberoniaAurea.RatkinOrder.DataLibrary;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_BranchPopulationChange : QuestNode
{
    [NoTranslate]
    public SlateRef<string> inSignalTrigger;
    [NoTranslate]
    public SlateRef<IEnumerable<string>> inSignalsAmountChange;

    public SlateRef<Branch> branch;
    public SlateRef<int> defaultAmount;
    public SlateRef<float> defaultChangeFactor = 1f;

    public SlateRef<bool> isReward;
    public SlateRef<bool> isSingleReward;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        QuestPart_BranchPopulationChange questPart_BranchPopulationChange = new()
        {
            InSignalTrigger = inSignalTrigger.GetValue(slate) ?? slate.Get<string>(OARO_KeyLibrary_SlateStoreAs.inSignal),

            Branch = branch.GetValue(slate) ?? slate.Get<Branch>(OARO_KeyLibrary_SlateStoreAs.branch),
            Amount = defaultAmount.GetValue(slate),
            ChangeFactor = defaultChangeFactor.GetValue(slate)
        };
        IEnumerable<string> inSignalsAmountChange = this.inSignalsAmountChange.GetValue(slate);
        if (inSignalsAmountChange is not null)
        {
            questPart_BranchPopulationChange.InSignalAmountChange ??= [];
            foreach (string inSignal in inSignalsAmountChange)
            {
                questPart_BranchPopulationChange.InSignalAmountChange.Add(QuestGenUtility.HardcodedSignalWithQuestID(inSignal));
            }
        }

        QuestGen.quest.AddPart(questPart_BranchPopulationChange);

        if (isReward.GetValue(slate))
        {
            QuestPart_Choice questPart_Choice;
            Reward_BranchPopulation reward = new()
            {
                Branch = questPart_BranchPopulationChange.Branch,
                DefaultAmount = questPart_BranchPopulationChange.Amount,
                DefaultChangeFactor = questPart_BranchPopulationChange.ChangeFactor
            };

            if (isSingleReward.GetValue(slate))
            {
                questPart_Choice = new QuestPart_Choice()
                {
                    inSignalChoiceUsed = questPart_BranchPopulationChange.InSignalTrigger,
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

public class QuestPart_BranchPopulationChange : QuestPart
{
    public string InSignalTrigger;
    public List<string> InSignalAmountChange;

    public Branch Branch;
    public int Amount;
    public float ChangeFactor = 1f;

    private bool hasTriggerd;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Branch, "Branch");
        Scribe_Values.Look(ref Amount, "Amount", 0);
        Scribe_Values.Look(ref ChangeFactor, "ChangeFactor", 1f);

        Scribe_Values.Look(ref hasTriggerd, "hasTriggerd", defaultValue: false);

        Scribe_Values.Look(ref InSignalTrigger, "InSignalTrigger");
        Scribe_Collections.Look(ref InSignalAmountChange, "InSignalAmountChange", LookMode.Value);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        Branch = null;
        Amount = 0;
        ChangeFactor = 1f;
        InSignalTrigger = null;
        InSignalAmountChange = null;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (InSignalAmountChange?.Contains(signal.tag) ?? false)
        {
            if (signal.args.TryGetArg("POPULATION", out int change))
            {
                Amount += change;
            }
            if (signal.args.TryGetArg("CHANGEFACTOR", out float factor))
            {
                ChangeFactor *= factor;
            }
        }

        if (!hasTriggerd && signal.tag == InSignalTrigger)
        {
            Branch.PopulationHandler.Population += Mathf.RoundToInt(Amount * ChangeFactor);
            hasTriggerd = true;
        }
    }
}