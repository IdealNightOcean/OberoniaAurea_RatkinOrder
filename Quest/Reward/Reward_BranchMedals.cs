using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

public class Reward_BranchMedals : Reward
{
    public int Amount;
    public Branch Branch;
    public List<BranchMedalDef> PotentialDefs;

    public override IEnumerable<GenUI.AnonymousStackElement> StackElements
    {
        get
        {
            Color color = Branch.RatkinOrder.Color;
            yield return QuestPartUtility.GetStandardRewardStackElement(label: "OARO_Reward_BranchMedal".Translate(Branch.Name).Colorize(color) + " " + ((int)Amount).ToStringWithSign().Colorize(color),
                                                                        iconDrawer: delegate (Rect r)
                                                                        {
                                                                            GUI.DrawTexture(r, null);
                                                                            GUI.color = Color.white;
                                                                        },
                                                                        tipGetter: () => "OARO_Reward_BranchMedalTip".Translate(Branch.Name).Resolve().Colorize(color));
        }
    }

    public override void InitFromValue(float rewardValue, RewardsGeneratorParams parms, out float valueActuallyUsed)
    {
        Amount = (short)Mathf.Max(Amount, 0);
        valueActuallyUsed = Amount;
    }

    public override IEnumerable<QuestPart> GenerateQuestParts(int index, RewardsGeneratorParams parms, string customLetterLabel, string customLetterText, RulePack customLetterLabelRules, RulePack customLetterTextRules)
    {
        yield return new QuestPart_GiveBranchMedal()
        {
            InSignalTrigger = QuestGen.slate.Get<string>("inSignal"),
            Branch = Branch ?? QuestGen.slate.Get<Branch>(KeyLibrary_SlateStoreAs.Branch),
            Count = Amount,
            PotentialDefs = [.. PotentialDefs]
        };
    }

    public override string GetDescription(RewardsGeneratorParams parms) => "OARO_Reward_BranchMedalDesc".Translate(Branch.Name, Amount).Resolve();

    public override string ToString() => $"{GetType().Name} (RatkinOrder={Branch.Name}, amount={Amount})";

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref Amount, nameof(Amount), 0);
        Scribe_References.Look(ref Branch, nameof(Branch));
        Scribe_Collections.Look(ref PotentialDefs, nameof(PotentialDefs), LookMode.Def);
    }
}