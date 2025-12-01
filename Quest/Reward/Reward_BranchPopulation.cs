using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

public class Reward_BranchPopulation : Reward
{
    public Branch Branch;
    public int DefaultAmount;
    public float DefaultChangeFactor = 1f;

    public override IEnumerable<GenUI.AnonymousStackElement> StackElements
    {
        get
        {
            yield return QuestPartUtility.GetStandardRewardStackElement(label: "OARO_Reward_BranchPopulation".Translate(Branch.Name.Named(KeyLibrary_FormatArgName.BranchName)),
                                                                        iconDrawer: delegate (Rect r)
                                                                        {
                                                                            GUI.DrawTexture(r, null);
                                                                            GUI.color = Color.white;
                                                                        },
                                                                        tipGetter: () => "OARO_Reward_BranchPopulationTip".Translate(Branch.Name.Named(KeyLibrary_FormatArgName.BranchName)).Resolve());
        }
    }

    public override void InitFromValue(float rewardValue, RewardsGeneratorParams parms, out float valueActuallyUsed)
    {
        valueActuallyUsed = DefaultAmount;
    }

    public override IEnumerable<QuestPart> GenerateQuestParts(int index, RewardsGeneratorParams parms, string customLetterLabel, string customLetterText, RulePack customLetterLabelRules, RulePack customLetterTextRules)
    {
        yield return new QuestPart_BranchPopulationChange()
        {
            InSignalTrigger = QuestGen.slate.Get<string>("inSignal"),
            Branch = Branch ?? QuestGen.slate.Get<Branch>(KeyLibrary_SlateStoreAs.Branch),
            Amount = DefaultAmount,
            ChangeFactor = DefaultChangeFactor
        };
    }

    public override string GetDescription(RewardsGeneratorParams parms) => "OARO_Reward_BranchPopulationDesc".Translate(Branch.Name.Named(KeyLibrary_FormatArgName.BranchName)).Resolve();

    public override string ToString() => $"{GetType().Name} (Branch={Branch.Name})";

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Branch, "Branch");
        Scribe_Values.Look(ref DefaultAmount, "DefaultAmount", 0);
        Scribe_Values.Look(ref DefaultChangeFactor, "DefaultChangeFactor", 1f);
    }
}