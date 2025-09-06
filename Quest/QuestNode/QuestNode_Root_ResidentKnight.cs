using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_Root_ResidentKnight : QuestNode_Root_RefugeeBase
{
    protected override void InitQuestParameter()
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

        RatkinOrder ratkinOrder = QuestGen.slate.Get<RatkinOrder>(KeyLibrary_SlateStoreAs.RatkinOrder);
        if (ratkinOrder.ReformationManager.HasReformation(null))
        {
            questParameter.questDurationTicks = 120 * 60000;
        }

        QuestPart_CriticalRatkinOrder questPart_CriticalRatkinOrder = new()
        {
            RatkinOrder = ratkinOrder,
            EndQuest = true,
            EndOutcome = QuestEndOutcome.Unknown
        };
        QuestGen.quest.AddPart(questPart_CriticalRatkinOrder);
        QuestPart_InvolvedRatkinOrders.AddInvolvedRatkinOrder(QuestGen.quest, ratkinOrder);
    }

    protected override void PostPawnGenerated(Pawn pawn)
    {
        base.PostPawnGenerated(pawn);
        pawn.health.GetOrAddHediff(OARO_HediffDefOf.OARO_Hediff_ResidentKnight);
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