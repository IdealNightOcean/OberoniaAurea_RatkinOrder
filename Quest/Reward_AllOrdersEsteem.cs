using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

public class Reward_AllOrdersEsteem : Reward
{
    public int amount;
    public bool showPlayerChangeMessage = true;
    public string reason;

    public override IEnumerable<GenUI.AnonymousStackElement> StackElements
    {
        get
        {
            yield return QuestPartUtility.GetStandardRewardStackElement(label: "OARO_AllOrdersEsteem".Translate() + " " + amount.ToStringWithSign(),
                                                                        iconDrawer: delegate (Rect r)
                                                                        {
                                                                            GUI.DrawTexture(r, null);
                                                                            GUI.color = Color.white;
                                                                        },
                                                                        tipGetter: () => "OARO_AllOrdersEsteemTip".Translate().Resolve());
        }
    }

    public override void InitFromValue(float rewardValue, RewardsGeneratorParams parms, out float valueActuallyUsed)
    {
        amount = Mathf.Max(amount, 0);
        valueActuallyUsed = amount;
    }

    public override IEnumerable<QuestPart> GenerateQuestParts(int index, RewardsGeneratorParams parms, string customLetterLabel, string customLetterText, RulePack customLetterLabelRules, RulePack customLetterTextRules)
    {
        yield return new QuestPart_AllOrdersEsteemChange(inSignalTrigger: QuestGen.slate.Get<string>("inSignal"), amount, showPlayerChangeMessage, reason);
    }

    public override string GetDescription(RewardsGeneratorParams parms) => "OARO_Reward_AllOrdersEsteemTip".Translate(amount).Resolve();

    public override string ToString() => $"{GetType().Name} (amount={amount})";

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref amount, "amount", 0);
        Scribe_Values.Look(ref showPlayerChangeMessage, "showPlayerChangeMessage", defaultValue: true);
        Scribe_Values.Look(ref reason, "reason");
    }
}
