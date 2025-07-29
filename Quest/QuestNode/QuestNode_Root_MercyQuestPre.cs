using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_Root_MercyQuestPre : QuestNode
{
    protected override bool TestRunInt(Slate slate)
    {
        return slate.TryGet(KeyLibrary_SlateStoreAs.MercyQuestStoreAs, out QuestScriptDef _);
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        Quest quest = QuestGen.quest;
        string rootInSignal = slate.Get<string>("inSignal");

        slate.TryGet(KeyLibrary_SlateStoreAs.MercyQuestStoreAs, out QuestScriptDef mercyQuest);

        slate.TryGet(KeyLibrary_SlateStoreAs.SubRatkinFactionDefStoreAs, out FactionDef subFactionDef);
        slate.TryGet(KeyLibrary_SlateStoreAs.ParentRatkinFactionDefStoreAs, out FactionDef parentFactionDef);
        slate.TryGet(KeyLibrary_SlateStoreAs.ParentRatkinFactionStoreAs, out Faction parentFaction);

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
        slate.Set(KeyLibrary_SlateStoreAs.SubRatkinFactionStoreAs, subFaction);

        Pawn helpSeeker = quest.GeneratePawn(OARO_PawnKindDefOf.RatkinColonist, subFaction, allowPregnant: false, forceGenerateNewPawn: true);
        slate.Set("helpSeeker", helpSeeker);
        quest.PawnsArrive([helpSeeker], inSignal: rootInSignal, map.Parent, arrivalMode: PawnsArrivalModeDefOf.EdgeWalkIn);

        string inSignalAccept = QuestGenUtility.HardcodedSignalWithQuestID("helpSeeker.AcceptMercyQuest");
        string inSignalReject = QuestGenUtility.HardcodedSignalWithQuestID("helpSeeker.RejectMercyQuest");

        QuestPart_LordJob_HelpSeeker questPart_LordJob_HelpSeeker = new()
        {
            inSignal = rootInSignal,
            inSignalAccept = inSignalAccept,
            ininSignalReject = inSignalReject,

            subFaction = subFaction,
            parentFaction = parentFaction,
            parentFactionDef = parentFactionDef,

            talkWith = helpSeeker,
            mapOfPawn = helpSeeker,
            pawns = [helpSeeker],

            mercyQuestDef = mercyQuest,
            durationTicks = 60000
        };
        quest.AddPart(questPart_LordJob_HelpSeeker);

        string outSignalResolved = QuestGenUtility.HardcodedSignalWithQuestID("MercyQuest_Resolved");
        quest.SignalPassAny(inSignals: [inSignalAccept, inSignalReject], outSignal: outSignalResolved);
        quest.Delay(delayTicks: 60000, inner: null, inSignalDisable: outSignalResolved, outSignalComplete: inSignalReject);
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
            inSignalTrigger = inSignalReject,
            change = -1,
            reason = "OARO_RejctMercyQuest".Translate()
        };
        quest.AddPart(questPart_AllOrdersEsteemChange_Reject);
        QuestPart_AllOrdersEsteemChange questPart_AllOrdersEsteemChange_PawnNegative = new()
        {
            inSignalTrigger = inSignalPawnNegative,
            change = -10,
            reason = "OARO_HarmingHelpSeeker".Translate()
        };
        quest.AddPart(questPart_AllOrdersEsteemChange_PawnNegative);

        quest.End(QuestEndOutcome.Fail, inSignal: inSignalPawnNegative);
    }
}