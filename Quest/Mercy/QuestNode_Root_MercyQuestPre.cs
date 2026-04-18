using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_Root_MercyQuestPre : QuestNode
{
    protected override bool TestRunInt(Slate slate) => slate.TryGet(KeyLibrary_SlateStoreAs.mercyQuestDef, out MercyQuestDef _);

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        Quest quest = QuestGen.quest;
        Map map = slate.Get<Map>("map") ?? QuestGen_Get.GetMap();
        if (map is null)
        {
            QuestGen_End.End(quest, QuestEndOutcome.Unknown, sendStandardLetter: false, playSound: false);
            return;
        }
        slate.Set("map", map);
        slate.TryGet(KeyLibrary_SlateStoreAs.mercyQuestDef, out MercyQuestDef mercyQuestDef);

        slate.TryGet(KeyLibrary_SlateStoreAs.subFactionDef, out FactionDef subFactionDef);
        slate.TryGet(KeyLibrary_SlateStoreAs.parentFactionDef, out FactionDef parentFactionDef);
        slate.TryGet(KeyLibrary_SlateStoreAs.parentFaction, out Faction parentFaction);

        Faction subFaction = ModUtility.GenerateSubRatkinFaction(subFactionDef, parentFactionDef, parentFaction, addToManager: true);
        if (subFaction is null)
        {
            QuestGen_End.End(quest, QuestEndOutcome.Unknown, sendStandardLetter: false, playSound: false);
            return;
        }

        quest.AddPart(new QuestPart_PreMercyQuestCleaner());

        slate.Set(KeyLibrary_SlateStoreAs.subFaction, subFaction);

        string inSignalForceInterrupt = QuestGenUtility.HardcodedSignalWithQuestID("HelpSeeker_ForceInterrupt");
        QuestPart_MercyQuestPre_DangerConfirm questPart_MercyQuestPre_DangerConfirm = new()
        {
            InSignal = slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal),
            OutSignalForceInterrupt = inSignalForceInterrupt,

            Map = map,
            SubFaction = subFaction,
        };
        quest.AddPart(questPart_MercyQuestPre_DangerConfirm);
        quest.End(QuestEndOutcome.Unknown, inSignal: inSignalForceInterrupt);

        PawnKindDef pawnKindDef = slate.Get<PawnKindDef>(KeyLibrary_SlateStoreAs.helpSeekerPawnKind);
        Pawn helpSeeker = quest.GeneratePawn(pawnKindDef, subFaction, allowPregnant: false, forceGenerateNewPawn: true);
        slate.Set(KeyLibrary_SlateStoreAs.helpSeeker, helpSeeker);

        string inSignalMakePawnArrival = QuestGenUtility.HardcodedSignalWithQuestID("MakePawnArrival");
        quest.PawnsArrive(
            pawns: [helpSeeker],
            inSignal: inSignalMakePawnArrival,
            mapParent: map.Parent,
            arrivalMode: PawnsArrivalModeDefOf.EdgeWalkIn,
            customLetterLabel: "OARO_HelpSeeker_Alert".Translate(),
            customLetterText: "OARO_HelpSeeker_AlertExp".Translate());

        string inSignalAccept = QuestGenUtility.HardcodedSignalWithQuestID("HelpSeeker_AcceptMercyQuest");
        string inSignalTransfer = QuestGenUtility.HardcodedSignalWithQuestID("HelpSeeker_RejectMercyQuest_Transfer");
        string inSignalTransferWithHelp = QuestGenUtility.HardcodedSignalWithQuestID("HelpSeeker_RejectMercyQuest_TransferWithHelp");
        string inSignalReject = QuestGenUtility.HardcodedSignalWithQuestID("HelpSeeker_RejectMercyQuest");
        string inSignalForceTriggerTalk = QuestGenUtility.HardcodedSignalWithQuestID("HelpSeeker_ForceTriggerTalk");

        string inSignalTalkTextReset = QuestGenUtility.HardcodedSignalWithQuestID("HelpSeeker_TalkTextResetSignal");

        if (RatkinOrderSettings.EnableAIContent)
        {
            quest.SignalPass(inSignal: inSignalTalkTextReset, outSignal: inSignalMakePawnArrival);
        }
        int arrivalDelayTick = RatkinOrderSettings.EnableAIContent ? 30000 : 60;
        quest.Delay(delayTicks: arrivalDelayTick, inner: null, inSignalDisable: inSignalMakePawnArrival, outSignalComplete: inSignalMakePawnArrival);

        float delayMulti = OrderStationHandler.Instance.OrderStationLevel switch
        {
            < 4 => 1f,
            4 => 1.25f,
            5 => 1.5f,
            6 => 1.75f,
            7 => 2f,
            _ => 2f
        };
        int helpSeekerLeaveDelay = (int)GenMath.RoundTo(60000 * delayMulti, 2500) + 60;

        QuestPart_LordJob_HelpSeeker questPart_LordJob_HelpSeeker = new()
        {
            inSignal = inSignalMakePawnArrival,
            InSignalForceTriggerTalk = inSignalForceTriggerTalk,

            OutSignalAccept = inSignalAccept,
            OutSignalTransfer = inSignalTransfer,
            OutSignalTransferWithHelp = inSignalTransferWithHelp,
            OutSignalReject = inSignalReject,

            OutSignalTalkTextReset = inSignalTalkTextReset,

            mapOfPawn = helpSeeker,
            pawns = [helpSeeker],

            DurationTicks = helpSeekerLeaveDelay,

            MercyQuestDef = mercyQuestDef,
            SubFaction = subFaction,
            ParentFaction = parentFaction
        };
        questPart_LordJob_HelpSeeker.SetTalkWith(helpSeeker);
        quest.AddPart(questPart_LordJob_HelpSeeker);

        TriggerMercyQuestPart(inSignalAccept, inSignalReject, subFaction, parentFaction, helpSeeker);

        string outSignalResolved = QuestGenUtility.HardcodedSignalWithQuestID("MercyQuest_Resolved");
        quest.SignalPassAny(
            inSignals: [inSignalAccept, inSignalTransfer, inSignalTransferWithHelp, inSignalReject],
            outSignal: outSignalResolved);

        quest.Delay(
            delayTicks: helpSeekerLeaveDelay,
            inner: null,
            inSignalEnable: inSignalMakePawnArrival,
            inSignalDisable: outSignalResolved,
            outSignalComplete: inSignalForceTriggerTalk,
            debugLabel: "强制决定");

        string outSignalMakeLeave = QuestGenUtility.HardcodedSignalWithQuestID("MercyQuest_MakeLeave");
        quest.Delay(
            delayTicks: 60,
            inner: null,
            inSignalEnable: inSignalForceTriggerTalk,
            inSignalDisable: outSignalResolved,
            outSignalComplete: outSignalMakeLeave,
            debugLabel: "强制离开");

        quest.Alert(label: "OARO_HelpSeeker_Alert".Translate(),
                    explanation: "OARO_HelpSeeker_AlertExp".Translate(),
                    lookTargets: helpSeeker,
                    inSignalEnable: inSignalMakePawnArrival,
                    inSignalDisable: outSignalResolved);

        string inSignalPawnNegative = QuestGenUtility.HardcodedSignalWithQuestID("HelpSeeker_Negative");

        QuestPart_PawnNegativeSiganl questPart_PawnNegativeSiganl = new()
        {
            negativeSiganls = OAFrame_QuestUtility.GetCommonPawnNegativeSiganls(addTag: true, tagToAdd: "helpSeeker"),
            outSignal = inSignalPawnNegative,
            outOnlyOnce = true
        };
        quest.AddPart(questPart_PawnNegativeSiganl);

        quest.Leave(pawns: [helpSeeker], inSignal: outSignalMakeLeave, leaveOnCleanup: true);
        quest.End(QuestEndOutcome.Success, inSignal: QuestGenUtility.HardcodedSignalWithQuestID("helpSeeker.LeftMap"));

        QuestPart_AllOrdersEsteemChange questPart_AllOrdersEsteemChange_Reject = new()
        {
            InSignalTrigger = inSignalReject,
            Change = -1,
            Reason = "OARO_RejctMercyQuest".Translate()
        };
        quest.AddPart(questPart_AllOrdersEsteemChange_Reject);
        QuestPart_AllOrdersEsteemChange questPart_AllOrdersEsteemChange_PawnNegative = new()
        {
            InSignalTrigger = inSignalPawnNegative,
            Change = -5,
            Reason = "OARO_HarmingHelpSeeker".Translate()
        };
        quest.AddPart(questPart_AllOrdersEsteemChange_PawnNegative);

        quest.End(QuestEndOutcome.Fail, inSignal: inSignalPawnNegative);

        //5日强制结束任务
        quest.Delay(delayTicks: 5 * 60000,
            inner: delegate
            {
                QuestGen_End.End(quest, QuestEndOutcome.Unknown);
            });
    }

    protected virtual void TriggerMercyQuestPart(string acceptSignal, string rejectSignal, Faction subFaction, Faction parentFaction, Pawn helpSeeker)
    {
        QuestPart_TriggerMercyQuest questPart_TriggerMercyQuest = new()
        {
            InSignalAccept = acceptSignal,
            InSignalReject = rejectSignal,

            MercyQuestDef = QuestGen.slate.Get<MercyQuestDef>(KeyLibrary_SlateStoreAs.mercyQuestDef),

            SubFaction = subFaction,
            ParentFaction = parentFaction,

            HelpSeeker = helpSeeker
        };
        QuestGen.quest.AddPart(questPart_TriggerMercyQuest);
    }
}