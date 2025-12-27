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
        string rootInSignal = slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal);
        Map map = slate.Get<Map>("map") ?? QuestGen_Get.GetMap();
        if (map is null)
        {
            quest.End(QuestEndOutcome.Unknown, inSignal: null);
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
            quest.End(QuestEndOutcome.Unknown, inSignal: null);
            return;
        }

        quest.AddPart(new QuestPart_PreMercyQuestCleaner());

        slate.Set(KeyLibrary_SlateStoreAs.subFaction, subFaction);

        PawnKindDef pawnKindDef = slate.Get<PawnKindDef>(KeyLibrary_SlateStoreAs.helpSeekerPawnKind);
        Pawn helpSeeker = quest.GeneratePawn(pawnKindDef, subFaction, allowPregnant: false, forceGenerateNewPawn: true);
        slate.Set(KeyLibrary_SlateStoreAs.helpSeeker, helpSeeker);
        quest.PawnsArrive([helpSeeker], inSignal: rootInSignal, map.Parent, arrivalMode: PawnsArrivalModeDefOf.EdgeWalkIn);

        string inSignalAccept = QuestGenUtility.HardcodedSignalWithQuestID("AcceptMercyQuest");
        string inSignalReject = QuestGenUtility.HardcodedSignalWithQuestID("RejectMercyQuest");

        float delayMulti = OrderHallHandler.Instance.OrderHallLevel switch
        {
            < 4 => 1f,
            4 => 1.25f,
            5 => 1.5f,
            6 => 1.75f,
            7 => 2f,
            _ => 2f
        };
        int helpSeekerLeaveDelay = (int)GenMath.RoundTo(60000 * delayMulti, 2500);

        QuestPart_LordJob_HelpSeeker questPart_LordJob_HelpSeeker = new()
        {
            inSignal = rootInSignal,
            OutSignalAccept = inSignalAccept,
            OutSignalReject = inSignalReject,

            mapOfPawn = helpSeeker,
            pawns = [helpSeeker],

            TalkWith = helpSeeker,
            DurationTicks = helpSeekerLeaveDelay,

            MercyQuestDef = mercyQuestDef,
            SubFaction = subFaction,
            ParentFaction = parentFaction
        };
        quest.AddPart(questPart_LordJob_HelpSeeker);

        TriggerMercyQuestPart(inSignalAccept, inSignalReject, subFaction, parentFaction, helpSeeker);

        string outSignalResolved = QuestGenUtility.HardcodedSignalWithQuestID("MercyQuest_Resolved");
        quest.SignalPassAny(inSignals: [inSignalAccept, inSignalReject], outSignal: outSignalResolved);
        quest.Delay(delayTicks: helpSeekerLeaveDelay, inner: null, inSignalDisable: outSignalResolved, outSignalComplete: inSignalReject);
        quest.Alert(label: "OARO_HelpSeeker_Alert".Translate(),
                    explanation: "OARO_HelpSeeker_AlertExp".Translate(),
                    lookTargets: helpSeeker,
                    inSignalDisable: outSignalResolved);

        string inSignalPawnNegative = QuestGenUtility.HardcodedSignalWithQuestID("HelpSeeker_Negative");

        QuestPart_PawnNegativeSiganl questPart_PawnNegativeSiganl = new()
        {
            negativeSiganls = OAFrame_QuestUtility.GetCommonPawnNegativeSiganls(addTag: true, tagToAdd: "helpSeeker"),
            outSignal = inSignalPawnNegative,
            outOnlyOnce = true
        };
        quest.AddPart(questPart_PawnNegativeSiganl);

        quest.Leave(pawns: [helpSeeker], inSignal: outSignalResolved, leaveOnCleanup: true);
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
            Change = -10,
            Reason = "OARO_HarmingHelpSeeker".Translate()
        };
        quest.AddPart(questPart_AllOrdersEsteemChange_PawnNegative);

        quest.End(QuestEndOutcome.Fail, inSignal: inSignalPawnNegative);
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