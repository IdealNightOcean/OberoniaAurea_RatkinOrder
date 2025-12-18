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
    public RatkinOrder RatkinOrder;
    public int Count;
    public MapParent MapParent;
    public bool GiveToCaravan;

    public override IEnumerable<GenUI.AnonymousStackElement> StackElements
    {
        get
        {
            yield return QuestPartUtility.GetStandardRewardStackElement(label: "OARO_Reward_OrderRecommendation".Translate(RatkinOrder.Name.Named(KeyLibrary_FormatArgName.OrderName)) + " " + Count.ToStringWithSign(),
                                                                        iconDrawer: delegate (Rect r)
                                                                        {
                                                                            GUI.DrawTexture(r, null); //OARO_ThingDefOf.OARO_OrderRecommendation.uiIcon
                                                                            GUI.color = Color.white;
                                                                        },
                                                                        tipGetter: () => "OARO_Reward_OrderRecommendationTip".Translate(RatkinOrder.Name.Named(KeyLibrary_FormatArgName.OrderName)).Resolve());
        }
    }

    public override void InitFromValue(float rewardValue, RewardsGeneratorParams parms, out float valueActuallyUsed)
    {
        Count = Mathf.Max(Count, 0);
        valueActuallyUsed = Count;
    }

    public override IEnumerable<QuestPart> GenerateQuestParts(int index, RewardsGeneratorParams parms, string customLetterLabel, string customLetterText, RulePack customLetterLabelRules, RulePack customLetterTextRules)
    {
        yield return new QuestPart_OrderRecommendation()
        {
            InSignalTrigger = QuestGen.slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal),
            RatkinOrder = RatkinOrder ?? QuestGen.slate.Get<RatkinOrder>(KeyLibrary_SlateStoreAs.ratkinOrder),
            Count = Count,
            MapParent = MapParent,
            GiveToCaravan = GiveToCaravan,
        };
    }

    public override string GetDescription(RewardsGeneratorParams parms) => "OARO_Reward_OrderRecommendationDesc".Translate(RatkinOrder.Name.Named(KeyLibrary_FormatArgName.OrderName), Count.Named(KeyLibrary_FormatArgName.Count)).Resolve();

    public override string ToString() => $"{GetType().Name} (RatkinOrder={RatkinOrder.NameColored}, count={Count})";

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref Count, "Count", 0);
        Scribe_References.Look(ref RatkinOrder, "RatkinOrder");
        Scribe_References.Look(ref MapParent, "MapParent");
        Scribe_Values.Look(ref GiveToCaravan, "GiveToCaravan", defaultValue: false);
    }
}
