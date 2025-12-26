using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_Root_KnightsVisit : QuestNode_Root_RefugeeKnightBase
{
    protected override PawnGroupKindDef PawnGroupKind => OARO_PawnGroupKindDefOf.OARO_KnightlyVisitor;
    protected override bool IsCombatant => true;
    protected override ThoughtDef ThoughtToAdd => OARO_ThoughtDefOf.OARO_Thought_VisitingKnight;
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

            questDurationTicks = 2 * 60000
        };

        Slate slate = QuestGen.slate;
        slate.Set(UniqueLeavingLetterSlate, true);

        if (slate.TryGet(KeyLibrary_SlateStoreAs.visitingKnightsDelay, out int visitDelay))
        {
            questParameter.arrivalDelayTicks = visitDelay;
        }
        if (slate.TryGet(KeyLibrary_SlateStoreAs.visitingKnightsDuration, out int visitDuration))
        {
            questParameter.questDurationTicks = visitDuration;
        }
        if (slate.TryGet(KeyLibrary_SlateStoreAs.visitingKnightsCount, out int visiterCount))
        {
            questParameter.LodgerCount = visiterCount;
        }

        QuestPart_KnightVisitWatcher questPart_KnightVisitWatcher = new()
        {
            KnightCount = questParameter.LodgerCount
        };
        QuestGen.quest.AddPart(questPart_KnightVisitWatcher);

        return InitRatkinOrder(initBranch: true);
    }

    protected override void PostPawnGenerated(Pawn pawn, string lodgerRecruitedSignal)
    {
        base.PostPawnGenerated(pawn, lodgerRecruitedSignal);
        pawn.workSettings.EnableAndInitializeIfNotAlreadyInitialized();
        pawn.workSettings.DisableAll();
        SetWorkPrioritySafe(pawn, WorkTypeDefOf.Firefighter, 2);
        SetWorkPrioritySafe(pawn, WorkTypeDefOf.Cleaning, 3);
        SetWorkPrioritySafe(pawn, WorkTypeDefOf.Handling, 3);
        SetWorkPrioritySafe(pawn, OARO_RimWorldDefOf.Patient, 2);

        Hediff hediff = pawn.health.GetOrAddHediff(OARO_HediffDefOf.OARO_Hediff_RecruitKnight);
        HediffComp_Disappears disappearsComp = hediff?.TryGetComp<HediffComp_Disappears>();
        if (disappearsComp is not null)
        {
            disappearsComp.ticksToDisappear = questParameter.arrivalDelayTicks + questParameter.questDurationTicks + 600;
        }
    }

    protected override void PawnArrival(string lodgerArrivalSignal)
    {
        base.PawnArrival(lodgerArrivalSignal);
        if (questParameter.arrivalDelayTicks > 0)
        {
            QuestGen.quest.Letter(LetterDefOf.PositiveEvent, label: "[arrivalDelayLabel]", text: "[arrivalDelayText]");
        }
    }

    protected override void SetQuestEndComp(QuestPart_OARefugeeInteractions questPart_Interactions, string failSignal, string delayFailSignal, string successSignal)
    {
        string inSignalPawnNegative = QuestGenUtility.HardcodedSignalWithQuestID("Lodger_Negative");
        QuestPart_PawnNegativeSiganl questPart_PawnNegativeSiganl = new()
        {
            negativeSiganls = OAFrame_QuestUtility.GetCommonPawnNegativeSiganls(addTag: true, tagToAdd: "lodgers"),
            outSignal = inSignalPawnNegative,
            outOnlyOnce = false
        };
        QuestGen.quest.AddPart(questPart_PawnNegativeSiganl);

        QuestPart_OrderEsteemChange questPart_OrderEsteemChangePawnNegative = new()
        {
            InSignalTrigger = inSignalPawnNegative,
            RatkinOrder = RatkinOrder,
            Change = -10,
            Reason = "OARO_VisitingKnightKilled".Translate(),
        };
        QuestGen.quest.AddPart(questPart_OrderEsteemChangePawnNegative);

        base.SetQuestEndComp(questPart_Interactions, failSignal, delayFailSignal, successSignal);
    }
}