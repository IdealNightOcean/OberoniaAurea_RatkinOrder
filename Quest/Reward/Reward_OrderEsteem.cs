using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

public class Reward_OrderEsteem : Reward
{
    public int Amount;
    public RatkinOrder RatkinOrder;
    public string Reason;

    public override IEnumerable<GenUI.AnonymousStackElement> StackElements
    {
        get
        {
            Color color = RatkinOrder.Color;
            yield return QuestPartUtility.GetStandardRewardStackElement(label: "OARO_Reward_OrderEsteem".Translate(RatkinOrder.Name.Named(OARO_KeyLibrary_FormatArgName.OrderName)).Colorize(color) + " " + Amount.ToStringWithSign().Colorize(color),
                                                                        iconDrawer: delegate (Rect r)
                                                                        {
                                                                            GUI.DrawTexture(r, null);
                                                                            GUI.color = Color.white;
                                                                        },
                                                                        tipGetter: () => "OARO_Reward_OrderEsteemTip".Translate(RatkinOrder.Name.Named(OARO_KeyLibrary_FormatArgName.OrderName)).Resolve().Colorize(color));
        }
    }

    public override void InitFromValue(float rewardValue, RewardsGeneratorParams parms, out float valueActuallyUsed)
    {
        Amount = Mathf.Max(Amount, 0);
        valueActuallyUsed = Amount;
    }

    public override IEnumerable<QuestPart> GenerateQuestParts(int index, RewardsGeneratorParams parms, string customLetterLabel, string customLetterText, RulePack customLetterLabelRules, RulePack customLetterTextRules)
    {
        yield return new QuestPart_OrderEsteemChange()
        {
            InSignalTrigger = QuestGen.slate.Get<string>(OARO_KeyLibrary_SlateStoreAs.inSignal),
            RatkinOrder = RatkinOrder ?? QuestGen.slate.Get<RatkinOrder>(OARO_KeyLibrary_SlateStoreAs.ratkinOrder),
            Change = Amount,
            Reason = Reason,
        };
    }

    public override string GetDescription(RewardsGeneratorParams parms) => "OARO_Reward_OrderEsteemDesc".Translate(RatkinOrder.Name.Named(OARO_KeyLibrary_FormatArgName.OrderName), Amount.Named("Amount")).Resolve();

    public override string ToString() => $"{GetType().Name} (RatkinOrder={RatkinOrder.Name}, amount={Amount})";

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref Amount, nameof(Amount), 0);
        Scribe_References.Look(ref RatkinOrder, nameof(RatkinOrder));
        Scribe_Values.Look(ref Reason, nameof(Reason));
    }
}
