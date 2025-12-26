using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

public class Reward_FriendlyBranch : Reward
{
    public const int DefaultFriendlyDays = 40;

    public Branch Branch;
    public int DurationDays = DefaultFriendlyDays;

    public override IEnumerable<GenUI.AnonymousStackElement> StackElements
    {
        get
        {
            yield return QuestPartUtility.GetStandardRewardStackElement(label: "OARO_Reward_FriendlyBranch".Translate(Branch.Name.Named(KeyLibrary_FormatArgName.BranchName)),
                                                                        iconDrawer: delegate (Rect r)
                                                                        {
                                                                            GUI.DrawTexture(r, IconLibrary.SmallFriendlyIcon, ScaleMode.ScaleToFit);
                                                                            GUI.color = Color.white;
                                                                        },
                                                                        tipGetter: () => "OARO_Reward_FriendlyBranchTip".Translate(Branch.Name.Named(KeyLibrary_FormatArgName.BranchName), DurationDays.Named("DurationDays")).Resolve());
        }
    }

    public override void InitFromValue(float rewardValue, RewardsGeneratorParams parms, out float valueActuallyUsed)
    {
        valueActuallyUsed = DurationDays;
    }

    public override IEnumerable<QuestPart> GenerateQuestParts(int index, RewardsGeneratorParams parms, string customLetterLabel, string customLetterText, RulePack customLetterLabelRules, RulePack customLetterTextRules)
    {
        yield return new QuestPart_SetBranchToFriendly()
        {
            InSignalTrigger = QuestGen.slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal),
            Branch = Branch ?? QuestGen.slate.Get<Branch>(KeyLibrary_SlateStoreAs.branch),
            DurationDays = DurationDays
        };
    }

    public override string GetDescription(RewardsGeneratorParams parms) => "OARO_Reward_FriendlyBranchDesc".Translate(Branch.Name.Named(KeyLibrary_FormatArgName.BranchName), DurationDays.Named("DurationDays")).Resolve();

    public override string ToString() => $"{GetType().Name} (Branch={Branch.Name}, DurationDays={DurationDays})";

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Branch, "Branch");
        Scribe_Values.Look(ref DurationDays, "DurationDays", 0);
    }
}
