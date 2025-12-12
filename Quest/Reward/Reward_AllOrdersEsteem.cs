using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

public class Reward_AllOrdersEsteem : Reward
{
    public int Amount;
    public bool ShowPlayerChangeMessage = true;
    public string Reason;

    public override IEnumerable<GenUI.AnonymousStackElement> StackElements
    {
        get
        {
            yield return QuestPartUtility.GetStandardRewardStackElement(label: "OARO_Reward_AllOrdersEsteem".Translate() + " " + Amount.ToStringWithSign(),
                                                                        iconDrawer: delegate (Rect r)
                                                                        {
                                                                            GUI.DrawTexture(r, null);
                                                                            GUI.color = Color.white;
                                                                        },
                                                                        tipGetter: () => "OARO_Reward_AllOrdersEsteemTip".Translate().Resolve());
        }
    }

    public override void InitFromValue(float rewardValue, RewardsGeneratorParams parms, out float valueActuallyUsed)
    {
        Amount = Mathf.Max(Amount, 0);
        valueActuallyUsed = Amount;
    }

    public override IEnumerable<QuestPart> GenerateQuestParts(int index, RewardsGeneratorParams parms, string customLetterLabel, string customLetterText, RulePack customLetterLabelRules, RulePack customLetterTextRules)
    {
        yield return new QuestPart_AllOrdersEsteemChange(inSignalTrigger: QuestGen.slate.Get<string>("inSignal"), Amount, ShowPlayerChangeMessage, Reason);
    }

    public override string GetDescription(RewardsGeneratorParams parms) => "OARO_Reward_AllOrdersEsteemDesc".Translate(Amount).Resolve();

    public override string ToString() => $"{GetType().Name} (amount={Amount})";

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref Amount, "Amount", 0);
        Scribe_Values.Look(ref ShowPlayerChangeMessage, "ShowPlayerChangeMessage", defaultValue: true);
        Scribe_Values.Look(ref Reason, KeyLibrary_FormatArgName.Reason);
    }
}
