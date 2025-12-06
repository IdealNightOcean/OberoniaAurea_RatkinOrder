using RimWorld;
using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_Root_ResidentKnight : QuestNode_Root_RefugeeKnightBase
{
    protected override bool IsCombatant => true;
    private string ResignationSignal { get; set; }

    protected override bool InitQuestParameter()
    {
        questParameter = new()
        {
            allowAssaultColony = false,
            allowBadThought = false,
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
        ResignationSignal = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Resignation");

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

    protected override void SetPawnsLeaveComp(string lodgerArrivalSignal, string inSignalRemovePawn)
    {
        Quest quest = QuestGen.quest;

        quest.Signal(inSignal: ResignationSignal, delegate
        {
            quest.SignalPassWithFaction(questParameter.faction, null, delegate
            {
                quest.Letter(LetterDefOf.PositiveEvent, null, null, null, null, useColonistsFromCaravanArg: false, QuestPart.SignalListenMode.OngoingOnly, null, filterDeadPawnsFromLookTargets: false, "[lodgersLeavingLetterText]", null, "[lodgersLeavingLetterLabel]");
            });
            quest.Leave(questParameter.pawns, null, sendStandardLetter: false, leaveOnCleanup: false, inSignalRemovePawn, wakeUp: true);
        });
    }
}