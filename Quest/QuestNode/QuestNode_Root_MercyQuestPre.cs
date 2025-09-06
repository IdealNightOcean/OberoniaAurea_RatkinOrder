using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_Root_MercyQuestPre : QuestNode
{
    protected override bool TestRunInt(Slate slate)
    {
        return slate.TryGet(KeyLibrary_SlateStoreAs.MercyQuest, out QuestScriptDef _);
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        Quest quest = QuestGen.quest;
        string rootInSignal = slate.Get<string>("inSignal");

        slate.TryGet(KeyLibrary_SlateStoreAs.MercyQuest, out QuestScriptDef mercyQuest);

        slate.TryGet(KeyLibrary_SlateStoreAs.SubRatkinFactionDef, out FactionDef subFactionDef);
        slate.TryGet(KeyLibrary_SlateStoreAs.ParentRatkinFactionDef, out FactionDef parentFactionDef);
        slate.TryGet(KeyLibrary_SlateStoreAs.ParentRatkinFaction, out Faction parentFaction);

        subFactionDef ??= OARO_ModDefOf.OARO_Rakinia_Sub;
        Faction subFaction = ModUtility.GenerateSubRatkinFaction(subFactionDef, parentFactionDef, parentFaction, addToManager: true);
        Map map = QuestGen_Get.GetMap();
        if (map is null)
        {
            quest.End(QuestEndOutcome.Unknown, 0, null, null, sendStandardLetter: false);
            return;
        }

        if (subFaction is null)
        {
            quest.End(QuestEndOutcome.Unknown, 0, null, null, sendStandardLetter: false);
            return;
        }
        slate.Set("map", map);
        slate.Set(KeyLibrary_SlateStoreAs.SubRatkinFaction, subFaction);

        Pawn helpSeeker = quest.GeneratePawn(OARO_PawnKindDefOf.RatkinColonist, subFaction, allowPregnant: false, forceGenerateNewPawn: true);
        slate.Set("helpSeeker", helpSeeker);
        quest.PawnsArrive([helpSeeker], inSignal: rootInSignal, map.Parent, arrivalMode: PawnsArrivalModeDefOf.EdgeWalkIn);

        string inSignalAccept = QuestGenUtility.HardcodedSignalWithQuestID("helpSeeker.AcceptMercyQuest");
        string inSignalReject = QuestGenUtility.HardcodedSignalWithQuestID("helpSeeker.RejectMercyQuest");

        float delayMulti = OrderInteractionHandler.OrderHallLevel switch
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
            InSignalAccept = inSignalAccept,
            IninSignalReject = inSignalReject,

            SubFaction = subFaction,
            ParentFaction = parentFaction,
            ParentFactionDef = parentFactionDef,

            TalkWith = helpSeeker,
            mapOfPawn = helpSeeker,
            pawns = [helpSeeker],

            MmercyQuestDef = mercyQuest,
            DurationTicks = helpSeekerLeaveDelay
        };
        quest.AddPart(questPart_LordJob_HelpSeeker);

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
            negativeSiganls = QuestNode_PawnNegativeSiganl.GetCommonNegativeSiganls(addTag: true, tagToAdd: "helpSeeker"),
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
}