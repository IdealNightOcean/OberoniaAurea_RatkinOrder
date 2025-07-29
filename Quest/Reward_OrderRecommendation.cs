using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

public class Reward_OrderRecommendation : Reward
{
    public int count;
    public RatkinOrder order;
    public MapParent mapParent;
    public bool giveToCaravan;

    public override IEnumerable<GenUI.AnonymousStackElement> StackElements
    {
        get
        {
            Color color = order.Color;
            yield return QuestPartUtility.GetStandardRewardStackElement(label: "OARO_OrderRecommendation".Translate(order.Name).Colorize(color) + " " + count.ToStringWithSign(),
                                                                        iconDrawer: delegate (Rect r)
                                                                        {
                                                                            GUI.DrawTexture(r, null); //OARO_ThingDefOf.OARO_OrderRecommendation.uiIcon
                                                                            GUI.color = Color.white;
                                                                        },
                                                                        tipGetter: () => "OARO_OrderRecommendationTip".Translate(order.Name).Resolve().Colorize(color));
        }
    }

    public override void InitFromValue(float rewardValue, RewardsGeneratorParams parms, out float valueActuallyUsed)
    {
        count = Mathf.Max(count, 0);
        valueActuallyUsed = count;
    }

    public override IEnumerable<QuestPart> GenerateQuestParts(int index, RewardsGeneratorParams parms, string customLetterLabel, string customLetterText, RulePack customLetterLabelRules, RulePack customLetterTextRules)
    {
        yield return new QuestPart_OrderRecommendation()
        {
            inSignalTrigger = QuestGen.slate.Get<string>("inSignal"),
            order = order ?? QuestGen.slate.Get<RatkinOrder>(KeyLibrary_SlateStoreAs.RatkinOrderStoreAs),
            count = count,
            mapParent = mapParent,
            giveToCaravan = giveToCaravan,
        };
    }

    public override string GetDescription(RewardsGeneratorParams parms) => "OARO_Reward_OrderRecommendationTip".Translate(order.NameColored, count).Resolve();

    public override string ToString() => $"{GetType().Name} (RatkinOrder={order.NameColored}, count={count})";

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref count, "count", 0);
        Scribe_References.Look(ref order, "order");
        Scribe_References.Look(ref mapParent, "mapParent");
        Scribe_Values.Look(ref giveToCaravan, "giveToCaravan", defaultValue: false);
    }
}
