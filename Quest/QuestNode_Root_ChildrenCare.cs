using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_Root_ChildrenCare : QuestNode_Root_RefugeeBase
{
    protected override QuestParameter InitQuestParameter(Faction faction)
    {
        int lodgerCount = Rand.RangeInclusive(4, 6);
        return new QuestParameter(faction, QuestGen_Get.GetMap())
        {
            allowAssaultColony = false,
            LodgerCount = lodgerCount,
            ChildCount = lodgerCount,

            goodwillFailure = -20,
            goodwillSuccess = 20,
            rewardValueRange = new FloatRange(1000, 2000),

            questDurationTicks = Rand.RangeInclusive(8 * 60000, 12 * 60000),

            fixedPawnKind = OARO_ModDefOf.OARO_RatkinVillageChild,
            addMemory = OARO_ModDefOf.OARO_Thought_ChildrenCare
        };
    }

    protected override void SetQuestEndComp(QuestParameter questParameter, QuestPart_OARefugeeInteractions questPart_Interactions, string failSignal, string bigFailSignal, string successSignal)
    {
        Quest quest = questParameter.quest;
        quest.AddPart(new QuestPart_EsteemChangeAllOrders(failSignal, 0.2f));
        quest.AddPart(new QuestPart_EsteemChangeAllOrders(bigFailSignal, 0.2f));
        quest.AddPart(new QuestPart_EsteemChangeAllOrders(successSignal, 0.02f));
    }

}
