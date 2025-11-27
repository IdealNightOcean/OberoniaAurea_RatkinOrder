using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_Root_ResidentKnight : QuestNode_Root_RefugeeKnightBase
{
    protected override bool IsCombatant => true;

    protected override bool InitQuestParameter()
    {
        questParameter = new()
        {
            allowAssaultColony = false,
            allowBadThought = true,
            allowLeave = false,
            allowFutureReward = false,
            allowJoinOffer = false,

            LodgerCount = 1,

            goodwillSuccess = 0,
            goodwillFailure = -25,

            questDurationTicks = 60 * 60000
        };

        if (!InitRatkinOrder(initBranch: true))
        {
            return false;
        }

        if (ratkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder))
        {
            questParameter.questDurationTicks = 120 * 60000;
        }

        return true;
    }

    protected override void PawnArrival(string lodgerArrivalSignal)
    {
        base.PawnArrival(lodgerArrivalSignal);
        QuestPart_ResidentKnightWatcher questPart_ResidentKnightWatcher = new()
        {
            Knight = questParameter.pawns[0]
        };
        QuestGen.quest.AddPart(questPart_ResidentKnightWatcher);
    }
}