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
        Map map = slate.Get<Map>("map") ?? QuestGen_Get.GetMap();
        if (map is null)
        {
            quest.End(QuestEndOutcome.Unknown, inSignal: null);
            return;
        }
        slate.Set("map", map);
        slate.TryGet(KeyLibrary_SlateStoreAs.MercyQuest, out QuestScriptDef mercyQuest);

        slate.TryGet(KeyLibrary_SlateStoreAs.SubFactionDef, out FactionDef subFactionDef);
        slate.TryGet(KeyLibrary_SlateStoreAs.ParentFactionDef, out FactionDef parentFactionDef);
        slate.TryGet(KeyLibrary_SlateStoreAs.ParentFaction, out Faction parentFaction);

        subFactionDef ??= OARO_ModDefOf.OARO_Rakinia_Sub;
        Faction subFaction = ModUtility.GenerateSubRatkinFaction(subFactionDef, parentFactionDef, parentFaction, addToManager: true);
        if (subFaction is null)
        {
            quest.End(QuestEndOutcome.Unknown, inSignal: null);
            return;
        }

        slate.Set(KeyLibrary_SlateStoreAs.SubFaction, subFaction);

        PawnKindDef pawnKindDef = slate.Get<PawnKindDef>(KeyLibrary_SlateStoreAs.HelpSeekerPawnKind) ?? OARO_PawnKindDefOf.RatkinColonist;
        Pawn helpSeeker = quest.GeneratePawn(pawnKindDef, subFaction, allowPregnant: false, forceGenerateNewPawn: true);
        slate.Set(KeyLibrary_SlateStoreAs.HelpSeeker, helpSeeker);
        quest.PawnsArrive([helpSeeker], inSignal: rootInSignal, map.Parent, arrivalMode: PawnsArrivalModeDefOf.EdgeWalkIn);

        string inSignalAccept = QuestGenUtility.HardcodedSignalWithQuestID("AcceptMercyQuest");
        string inSignalReject = QuestGenUtility.HardcodedSignalWithQuestID("RejectMercyQuest");

        float delayMulti = GlobalOrderInteractionManager.OrderHallLevel switch
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

            MmercyQuestDef = mercyQuest,
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

            MmercyQuestDef = QuestGen.slate.Get<QuestScriptDef>(KeyLibrary_SlateStoreAs.MercyQuest),

            SubFaction = subFaction,
            ParentFaction = parentFaction,

            HelpSeeker = helpSeeker
        };
        QuestGen.quest.AddPart(questPart_TriggerMercyQuest);
    }
}