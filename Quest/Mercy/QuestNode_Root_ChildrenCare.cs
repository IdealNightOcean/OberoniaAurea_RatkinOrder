using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_Root_ChildrenCare : QuestNode_Root_RefugeeBase
{
    protected override PawnKindDef FixedPawnKind => OARO_PawnKindDefOf.OARO_RatkinVillageChild;
    protected override ThoughtDef ThoughtToAdd => OARO_ThoughtDefOf.OARO_Thought_ChildrenCare;

    protected override Faction GetOrGenerateFaction()
    {
        Slate slate = QuestGen.slate;
        Faction subFaction = slate.Get<Faction>(OARO_KeyLibrary_SlateStoreAs.subFaction);
        QuestPart_MercyQuestWatcher questPart_MercyQuestWatcher = new()
        {
            MercyQuestDef = slate.Get<MercyQuestDef>(OARO_KeyLibrary_SlateStoreAs.mercyQuestDef),
            SubFaction = subFaction,
            ParentFaction = slate.Get<Faction>(OARO_KeyLibrary_SlateStoreAs.parentFaction)
        };
        QuestGen.quest.AddPart(questPart_MercyQuestWatcher);

        slate.Set(IsMainFactionSlate, true);
        return subFaction;
    }

    protected override bool InitQuestParameter()
    {
        int lodgerCount = Rand.RangeInclusive(4, 6);
        questParameter = new QuestParameter()
        {
            allowAssaultColony = false,
            allowJoinOffer = false,

            LodgerCount = lodgerCount,
            ChildCount = lodgerCount,

            rewardValueRange = new FloatRange(1000, 2000),

            questDurationTicks = Rand.RangeInclusive(8 * 60000, 12 * 60000)
        };

        QuestGen.slate.Set("uniqueQuestDesc", true);
        QuestGen.slate.Set("uniqueLeavingLetter", true);

        return true;
    }

    protected override void AddQuestAward(QuestPart_Choice.Choice choice)
    {
        Reward_AllOrdersEsteem reward_Esteem = new()
        {
            Amount = 2,
            Reason = "OARO_Childcare".Translate()
        };
        Reward_OrderRecommendation reward_Recommendation = new()
        {
            Count = 1
        };
        choice.rewards.Add(reward_Esteem);
        choice.rewards.Add(reward_Recommendation);
    }

    protected override void SetQuestEndComp(QuestPart_OARefugeeInteractions questPart_Interactions, string failSignal, string delayFailSignal, string successSignal)
    {
        Quest quest = QuestGen.quest;
        quest.FactionHostileToOtherFaction(questParameter.faction, Faction.OfPlayer, outSignal: failSignal);
        quest.AddPart(new QuestPart_AllOrdersEsteemChange(failSignal, -20, reason: "OARO_HarmingChildren".Translate()));
        quest.AddPart(new QuestPart_AllOrdersEsteemChange(delayFailSignal, -20, reason: "OARO_HarmingChildren".Translate()));
        quest.AddPart(new QuestPart_AllOrdersEsteemChange(successSignal, 2, reason: "OARO_Childcare".Translate()));
        quest.AddPart(new QuestPart_OrderRecommendation()
        {
            InSignalTrigger = successSignal,
            Count = 1
        });
    }
}