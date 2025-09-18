using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_Root_KnightsVisit : QuestNode_Root_RefugeeKnightBase
{
    public override PawnKindDef FixedPawnKind => OARO_PawnKindDefOf.RatkinKnight;
    protected override ThoughtDef ThoughtToAdd => OARO_ThoughtDefOf.OARO_Thought_VisitingKnight;
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

            questDurationTicks = 2 * 60000
        };

        Slate slate = QuestGen.slate;
        slate.Set(UniqueLeavingLetterSlate, true);

        if (slate.TryGet(KeyLibrary_SlateStoreAs.VisitingKnightsDelay, out int visitDelay))
        {
            questParameter.arrivalDelayTicks = visitDelay;
        }
        if (slate.TryGet(KeyLibrary_SlateStoreAs.VisitingKnightsDuration, out int visitDuration))
        {
            questParameter.questDurationTicks = visitDuration;
        }
        if (slate.TryGet(KeyLibrary_SlateStoreAs.VisitingKnightsCount, out int visiterCount))
        {
            questParameter.LodgerCount = visiterCount;
        }

        Branch branch = slate.Get<Branch>(KeyLibrary_SlateStoreAs.Branch);
        QuestPart_InvolvedRatkinOrders.AddInvolvedRatkinOrder(QuestGen.quest, branch.RatkinOrder);
        QuestPart_CriticalBranch questPart_CriticalBranch = new()
        {
            Branch = branch,
            EndQuest = true,
            EndOutcome = QuestEndOutcome.Fail
        };
        QuestGen.quest.AddPart(questPart_CriticalBranch);

        QuestPart_KnightVisitWatcher questPart_KnightVisitWatcher = new()
        {
            KnightCount = questParameter.LodgerCount
        };
        QuestGen.quest.AddPart(questPart_KnightVisitWatcher);
    }

    protected override Faction GetOrGenerateFaction()
    {
        QuestGen.slate.Set("isMainFaction", true);
        return QuestGen.slate.Get<Faction>(KeyLibrary_SlateStoreAs.OrderFaction);
    }

    protected override void SetQuestEndComp(QuestPart_OARefugeeInteractions questPart_Interactions, string failSignal, string delayFailSignal, string successSignal)
    {
        string inSignalPawnNegative = QuestGenUtility.HardcodedSignalWithQuestID("Lodger_Negative");
        QuestPart_PawnNegativeSiganl questPart_PawnNegativeSiganl = new()
        {
            negativeSiganls = QuestNode_PawnNegativeSiganl.GetCommonNegativeSiganls(addTag: true, tagToAdd: "lodgers"),
            outSignal = inSignalPawnNegative,
            outOnlyOnce = false
        };
        QuestGen.quest.AddPart(questPart_PawnNegativeSiganl);

        QuestPart_OrderEsteemChange questPart_OrderEsteemChangePawnNegative = new()
        {
            InSignalTrigger = inSignalPawnNegative,
            RatkinOrder = QuestGen.slate.Get<RatkinOrder>(KeyLibrary_SlateStoreAs.RatkinOrder),
            Change = -10,
            Reason = "OARO_VisitingKnightKilled".Translate(),
        };
        QuestGen.quest.AddPart(questPart_OrderEsteemChangePawnNegative);

        base.SetQuestEndComp(questPart_Interactions, failSignal, delayFailSignal, successSignal);
    }
}