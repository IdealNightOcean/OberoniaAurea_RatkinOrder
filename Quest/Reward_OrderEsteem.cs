using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

public class Reward_OrderEsteem : Reward
{
    public int amount;
    public RatkinOrder order;
    public string reason;

    public override IEnumerable<GenUI.AnonymousStackElement> StackElements
    {
        get
        {
            Color color = order.Color;
            yield return QuestPartUtility.GetStandardRewardStackElement(label: "OARO_OrderEsteem".Translate(order.Name).Colorize(color) + " " + amount.ToStringWithSign().Colorize(color),
                                                                        iconDrawer: delegate (Rect r)
                                                                        {
                                                                            GUI.DrawTexture(r, null);
                                                                            GUI.color = Color.white;
                                                                        },
                                                                        tipGetter: () => "OARO_OrderEsteemTip".Translate(order.Name).Resolve().Colorize(color));
        }
    }

    public override void InitFromValue(float rewardValue, RewardsGeneratorParams parms, out float valueActuallyUsed)
    {
        amount = Mathf.Max(amount, 0);
        valueActuallyUsed = amount;
    }

    public override IEnumerable<QuestPart> GenerateQuestParts(int index, RewardsGeneratorParams parms, string customLetterLabel, string customLetterText, RulePack customLetterLabelRules, RulePack customLetterTextRules)
    {
        yield return new QuestPart_OrderEsteemChange()
        {
            inSignalTrigger = QuestGen.slate.Get<string>("inSignal"),
            order = order ?? QuestGen.slate.Get<RatkinOrder>(KeyLibrary_SlateStoreAs.RatkinOrderStoreAs),
            change = amount,
            reason = reason,
        };
    }

    public override string GetDescription(RewardsGeneratorParams parms) => "OARO_Reward_OrderEsteemTip".Translate(order.Name, amount).Resolve();

    public override string ToString() => $"{GetType().Name} (RatkinOrder={order.Name}, amount={amount})";

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref amount, "amount", 0);
        Scribe_References.Look(ref order, "order");
        Scribe_Values.Look(ref reason, "reason");
    }
}
