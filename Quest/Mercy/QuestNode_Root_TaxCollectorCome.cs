using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using OberoniaAurea_Frame.DataLibrary;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 
/// </summary>
internal sealed class QuestNode_Root_TaxCollectorCome : QuestNode
{
    protected override bool TestRunInt(Slate slate)
    {
        return QuestGen_Get.GetMap() is not null;
    }
    private (Faction parentFaction, Faction subFaction) GetFactions()
    {
        Faction parentFaction = QuestGen.slate.Get<Faction>(OARO_KeyLibrary_SlateStoreAs.parentFaction);
        parentFaction ??= OberoniaAurea_Frame.Utility.OAFrame_FactionUtility.FirstAvailableFactionOf(validationParams: FactionValidationParams.NonHostileNormalFaction,
                                                                         predicater: f => f.IsRatkinKindomFaction());
        if (parentFaction is null)
        {
            return (null, null);
        }
        Faction subFaction = QuestGen.slate.Get<Faction>(OARO_KeyLibrary_SlateStoreAs.subFaction);
        subFaction ??= ModUtility.GenerateSubRatkinFaction(subFactionDef: QuestGen.slate.Get<FactionDef>(OARO_KeyLibrary_SlateStoreAs.subFactionDef) ?? OARO_ModDefOf.OARO_SubRakinia_Neutral,
                                                           parentFactionDef: parentFaction.def,
                                                           parentFaction: parentFaction);
        return (parentFaction, subFaction);
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        Quest quest = QuestGen.quest;
        Map map = QuestGen_Get.GetMap();
        if (map is null)
        {
            QuestGen_End.End(quest, QuestEndOutcome.Unknown, sendStandardLetter: false, playSound: false);
            return;
        }

        (Faction parentFaction, Faction subFaction) = GetFactions();
        if (parentFaction is null || subFaction is null)
        {
            QuestGen_End.End(quest, QuestEndOutcome.Unknown, sendStandardLetter: false, playSound: false);
            return;
        }

        QuestPart_InvolvedFactions questPart_InvolvedFactions = new()
        {
            factions = [parentFaction, subFaction]
        };
        quest.AddPart(questPart_InvolvedFactions);
        quest.ReserveFaction(subFaction);

        slate.Set("faction", parentFaction);
        slate.Set(OARO_KeyLibrary_SlateStoreAs.subFaction, subFaction);
        slate.Set("map", map);

        //人物生成;

        Pawn collector = quest.GeneratePawn(OARO_PawnKindDefOf.RatkinNoble, parentFaction, allowPregnant: false, forceGenerateNewPawn: true);
        List<Pawn> pawns = [collector];

        List<Pawn> guards = [];
        for (int i = 0; i < 6; i++)
        {
            Pawn p = quest.GeneratePawn(OARO_PawnKindDefOf.RatkinDefender, parentFaction, allowPregnant: false, forceGenerateNewPawn: true);
            guards.Add(p);
        }

        pawns.AddRange(guards);
        slate.Set("collector", collector);
        slate.Set("guards", guards);
        slate.Set("pawns", pawns);
        slate.Set("pawnCount", pawns.Count);

        string inSignalLeave = QuestGenUtility.HardcodedSignalWithQuestID("collector.LeaveByOpt");
        string inSignalTreat = QuestGenUtility.HardcodedSignalWithQuestID("collector.TreatByOpt");
        string inSignalReject = QuestGenUtility.HardcodedSignalWithQuestID("collector.RejectByOpt");

        string outSignalTreatSuccess = QuestGenUtility.HardcodedSignalWithQuestID("Collector_TreatSuccess");
        string outSignalTreatFail = QuestGenUtility.HardcodedSignalWithQuestID("Collector_TreatFail");

        string outSignalResolved = QuestGenUtility.HardcodedSignalWithQuestID("Collector_Resolved");
        string outSignalExpired = QuestGenUtility.HardcodedSignalWithQuestID("Collector_Expired");

        quest.AnySignal(inSignals: [inSignalLeave, outSignalTreatSuccess], outSignals: [outSignalResolved]);

        string inSignalPawnArrival = QuestGenUtility.HardcodedSignalWithQuestID("Pawns_Arrival");
        quest.Delay(delayTicks: Rand.RangeInclusive(1 * 60000, 3 * 60000),
                    inner: delegate
                    {
                        quest.PawnsArrive(pawns, mapParent: map.Parent, arrivalMode: PawnsArrivalModeDefOf.EdgeWalkIn);
                    },
                    outSignalComplete: inSignalPawnArrival,
                    reactivatable: false,
                    expiryInfoPart: "OARO_TaxCollectorComeInfo".Translate(),
                    expiryInfoPartTip: "OARO_TaxCollectorComeInfoTip".Translate());

        string inSignalRemovePawn = QuestGenUtility.HardcodedSignalWithQuestID("Pawns_RemovePawn");
        QuestPart_PawnNegativeSiganl questPart_PawnNegativeSiganl = new()
        {
            negativeSiganls = OberoniaAurea_Frame.Utility.OAFrame_QuestUtility.GetCommonPawnNegativeSiganls(addTag: true, tagToAdd: "pawns"),
            outOnlyOnce = false,
            outSignal = inSignalRemovePawn
        };
        quest.AddPart(questPart_PawnNegativeSiganl);

        string inSignalForceTriggerTalk = QuestGenUtility.HardcodedSignalWithQuestID("Collector_ForceTriggerTalk");
        quest.Delay(delayTicks: 20000,
            inner: null,
            inSignalEnable: inSignalPawnArrival,
            inSignalDisable: outSignalResolved,
            outSignalComplete: inSignalForceTriggerTalk,
            reactivatable: false,
            debugLabel: "强制决定");
        quest.Delay(delayTicks: 60,
                    inner: null,
                    inSignalEnable: inSignalForceTriggerTalk,
                    inSignalDisable: outSignalResolved,
                    outSignalComplete: outSignalExpired,
                    reactivatable: false,
                    debugLabel: "征收官离开");

        string inSignalMakeLeave = QuestGenUtility.HardcodedSignalWithQuestID("Pawns_MakeLeave");
        quest.AnySignal(inSignals: [inSignalLeave, inSignalReject, outSignalExpired, inSignalRemovePawn], outSignals: [inSignalMakeLeave]);
        quest.Leave(pawns: pawns, inSignal: inSignalMakeLeave, leaveOnCleanup: false, inSignalRemovePawn: inSignalRemovePawn);

        quest.Alert(label: "OARO_TaxCollector_Alert".Translate(),
            explanation: "OARO_TaxCollector_AlertExp".Translate(parentFaction.Named(KeyLibrary_FormatArgName.FACTION)),
            lookTargets: pawns,
            inSignalEnable: inSignalPawnArrival,
            inSignalDisable: inSignalMakeLeave);

        string outSignalFail = QuestGenUtility.HardcodedSignalWithQuestID("Collector_QuestFail");
        quest.AnySignal(inSignals: [inSignalReject, outSignalExpired], outSignals: [outSignalFail]);
        QuestPart_AllOrdersEsteemChange questPart_AllOrdersEsteemChange_Fail = new()
        {
            InSignalTrigger = outSignalFail,
            Change = -1,
            Reason = "OARO_TaxCollectorCome_Fail".Translate()
        };
        quest.AddPart(questPart_AllOrdersEsteemChange_Fail);

        QuestPart_LordJob_TaxCollector questPart_LordJob_TaxCollector = new()
        {
            inSignal = inSignalPawnArrival,
            InSignalForceTriggerTalk = inSignalForceTriggerTalk,
            InSignalTreat = inSignalTreat,
            inSignalRemovePawn = inSignalRemovePawn,
            OutSignalQuestSuccess = outSignalTreatSuccess,
            OutSignalQuestFail = outSignalTreatFail,

            mapOfPawn = collector,

            faction = parentFaction,
            SubFaction = subFaction,
            DurationTicks = 20000 + 60, // 8小时
        };
        questPart_LordJob_TaxCollector.SetTalkWith(collector);
        questPart_LordJob_TaxCollector.pawns.AddRange(pawns);
        quest.AddPart(questPart_LordJob_TaxCollector);

        QuestPart_AllOrdersEsteemChange questPart_AllOrdersEsteemChange_Suucess = new()
        {
            InSignalTrigger = outSignalResolved,
            Change = 3,
            Reason = "OARO_PutOffTaxCollector".Translate()
        };

        quest.AddPart(questPart_AllOrdersEsteemChange_Suucess);

        quest.End(QuestEndOutcome.Fail, -25, parentFaction, inSignal: inSignalRemovePawn, sendStandardLetter: true);
        quest.End(QuestEndOutcome.Fail, -5, parentFaction, inSignal: outSignalFail, sendStandardLetter: true);
        quest.End(QuestEndOutcome.Success, inSignal: outSignalResolved, sendStandardLetter: true);
        quest.End(QuestEndOutcome.Unknown, inSignal: outSignalTreatFail, sendStandardLetter: true);
    }
}