using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_Root_ChildrenCare : QuestNode_Root_RefugeeBase
{
    protected override Faction GetOrGenerateFaction()
    {
        return ModUtility.GenerateSubRatkinFaction(OARO_ModDefOf.OARO_Rakinia_Sub, OARO_ModDefOf.Rakinia);
    }

    protected override void InitQuestParameter()
    {
        int lodgerCount = Rand.RangeInclusive(4, 6);
        questParameter = new QuestParameter()
        {
            allowAssaultColony = false,
            LodgerCount = lodgerCount,
            ChildCount = lodgerCount,

            goodwillFailure = -20,
            goodwillSuccess = 20,
            rewardValueRange = new FloatRange(1000, 2000),

            questDurationTicks = Rand.RangeInclusive(8 * 60000, 12 * 60000),

            fixedPawnKind = OARO_PawnKindDefOf.OARO_RatkinVillageChild,
            addMemory = OARO_ModDefOf.OARO_Thought_ChildrenCare
        };
    }

    protected override void AddQuestAward(QuestPart_Choice.Choice choice)
    {
        Reward_AllOrdersEsteem reward = new()
        {
            Amount = 2,
            Reason = "OARO_Childcare".Translate()
        };
        choice.rewards.Add(reward);
    }

    protected override void SetQuestEndComp(QuestPart_OARefugeeInteractions questPart_Interactions, string failSignal, string bigFailSignal, string successSignal)
    {
        Quest quest = QuestGen.quest;
        quest.FactionHostileToOtherFaction(questParameter.faction, Faction.OfPlayer, outSignal: failSignal);
        quest.AddPart(new QuestPart_AllOrdersEsteemChange(failSignal, -20, reason: "OARO_HarmingChildren".Translate()));
        quest.AddPart(new QuestPart_AllOrdersEsteemChange(bigFailSignal, -20, reason: "OARO_HarmingChildren".Translate()));
        quest.AddPart(new QuestPart_AllOrdersEsteemChange(successSignal, 2, reason: "OARO_Childcare".Translate()));
    }
}