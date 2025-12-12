using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_Root_KnightAssistanceCommon : QuestNode_Root_RefugeeKnightBase
{
    private PawnKindDef _fixedPawnKind;
    private ThoughtDef _thoughtToAdd;

    protected override PawnKindDef FixedPawnKind => _fixedPawnKind;
    protected override ThoughtDef ThoughtToAdd => _thoughtToAdd;

    protected override Branch Branch { get => RatkinOrder.BranchManager.AllBranches.RandomElement(); }

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
            ChildCount = 0,

            goodwillSuccess = 0,
            goodwillFailure = -25,
        };
        Slate slate = QuestGen.slate;
        questParameter.LodgerCount = slate.Get("assistantCount", defaultValue: 1);
        _fixedPawnKind = slate.Get<PawnKindDef>("assistantPawnkind", defaultValue: OARO_PawnKindDefOf.RatkinKnight);
        _thoughtToAdd = slate.Get<ThoughtDef>("thoughtToAdd", defaultValue: null);

        slate.Set(UniqueQuestDescSlate, true);
        slate.Set(UniqueLeavingLetterSlate, true);

        return InitRatkinOrder(initBranch: false);
    }

    protected override void ClearQuestParameter()
    {
        base.ClearQuestParameter();
        _fixedPawnKind = null;
        _thoughtToAdd = null;
    }

    protected override void SetPawnsLeaveComp(string lodgerArrivalSignal, string inSignalRemovePawn)
    {
        Quest quest = QuestGen.quest;

        string outSignalMakePawnsLeave = QuestGenUtility.HardcodedSignalWithQuestID("Lodger_MakeLeave");
        QuestPart_AssistKnighWatcher questPart_AssistKnighWatcher = new()
        {
            inSignalEnable = lodgerArrivalSignal,
            outSignalsCompleted = [outSignalMakePawnsLeave],
            delayTicks = 5 * 60000,

            InsignalRemovePawn = inSignalRemovePawn,
            ThoughtToAdd = ThoughtToAdd,
            RatkinOrder = RatkinOrder,

            expiryInfoPart = "GuestsDepartsIn".Translate(),
            expiryInfoPartTip = "GuestsDepartsOn".Translate(),
            debugLabel = "QuestDelay",

            Pawns = []
        };
        questPart_AssistKnighWatcher.Pawns.AddRange(questParameter.pawns);
        quest.AddPart(questPart_AssistKnighWatcher);

        quest.Letter(
            letterDef: LetterDefOf.PositiveEvent,
            inSignal: outSignalMakePawnsLeave,
            relatedFaction: questParameter.faction,
            signalListenMode: QuestPart.SignalListenMode.OngoingOnly,
            lookTargets: questParameter.pawns,
            filterDeadPawnsFromLookTargets: true,
            text: "[lodgersLeavingLetterText]",
            label: "[lodgersLeavingLetterLabel]");

        quest.Leave(
            pawns: questParameter.pawns,
            inSignal: outSignalMakePawnsLeave,
            sendStandardLetter: false,
            leaveOnCleanup: true,
            inSignalRemovePawn: inSignalRemovePawn,
            wakeUp: true);
    }

    protected override void SetQuestEndComp(QuestPart_OARefugeeInteractions questPart_Interactions, string failSignal, string delayFailSignal, string successSignal)
    {
        Quest quest = QuestGen.quest;

        string inSignalPawnNegative = QuestGenUtility.HardcodedSignalWithQuestID("Lodger_Negative");
        QuestPart_PawnNegativeSiganl questPart_PawnNegativeSiganl = new()
        {
            negativeSiganls = OAFrame_QuestUtility.GetCommonPawnNegativeSiganls(addTag: true, tagToAdd: "lodgers"),
            outSignal = inSignalPawnNegative,
            outOnlyOnce = false
        };
        quest.AddPart(questPart_PawnNegativeSiganl);

        QuestPart_OrderEsteemChange questPart_OrderEsteemChangePawnNegative = new()
        {
            InSignalTrigger = inSignalPawnNegative,
            RatkinOrder = RatkinOrder,
            Change = -20,
            Reason = "OARO_Harming_AssisrKnight".Translate()
        };
        quest.AddPart(questPart_OrderEsteemChangePawnNegative);

        base.SetQuestEndComp(questPart_Interactions, failSignal, delayFailSignal, successSignal);
    }
}