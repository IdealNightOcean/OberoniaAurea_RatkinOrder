using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 旅行商的失物
/// </summary>
public class QuestNode_Root_LostItemsOfTrader : QuestNode
{
    public static readonly IntRange SilverCountRange = new(5000, 8000);

    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        Quest quest = QuestGen.quest;

        Map map = slate.Get<Map>("map") ?? OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
        if (map is null)
        {
            QuestGen_End.End(quest, QuestEndOutcome.Unknown, sendStandardLetter: false, playSound: false);
            return;
        }

        slate.Set("map", map);

        Faction parentFaction = slate.Get<Faction>(KeyLibrary_SlateStoreAs.parentFaction);
        if (parentFaction is null)
        {
            FactionValidationParams validationParams = new()
            {
                AllyHostile = false
            };
            parentFaction = OAFrame_FactionUtility.RandomAvailableFactionOfDef(OARO_ModDefOf.Rakinia_TravelRatkin, validationParams);
        }

        QuestPart_MercyQuestWatcher questPart_MercyQuestWatcher = new()
        {
            ParentFaction = parentFaction
        };
        quest.AddPart(questPart_MercyQuestWatcher);

        IntVec3? spawnCell = null;
        if (slate.TryGet(KeyLibrary_SlateStoreAs.helpSeeker, out Pawn helpSeeker))
        {
            spawnCell = helpSeeker.PositionHeld;
        }
        int silverCount = SilverCountRange.RandomInRange;
        Thing silverLost = ThingMaker.MakeThing(ThingDefOf.Silver);
        silverLost.stackCount = silverCount;
        quest.SpawnThing(map, silverLost, cell: spawnCell, lookForSafeSpot: true, questLookTarget: false);

        if (parentFaction is null || Rand.Chance(0.5f))
        {
            slate.Set("hasFollowUp", false);
            NoFurtherAction();
        }
        else
        {
            slate.Set("hasFollowUp", true);
            FollowUpAction(map, parentFaction, silverCount);
        }
    }

    private void NoFurtherAction()
    {
        Quest quest = QuestGen.quest;
        quest.Delay(
            delayTicks: Rand.RangeInclusive(4, 6) * 60000,
            inner: delegate
            {
                quest.Letter(letterDef: LetterDefOf.PositiveEvent,
                             text: "[lostItemsOfTrader_NoFurtherText]",
                             label: "[lostItemsOfTrader_NoFurtherLabel]");
                QuestGen_End.End(quest, outcome: QuestEndOutcome.Success);
            });
    }

    private void FollowUpAction(Map map, Faction parentFaction, int silverCount)
    {
        Slate slate = QuestGen.slate;
        Quest quest = QuestGen.quest;

        List<Pawn> collectionTeam = QuestPart_CollectionTeam.GenerateCaravanMembers(OARO_ModDefOf.OARO_LostItemsOfTrader, parentFaction, map);
        if (collectionTeam.NullOrEmpty())
        {
            slate.Set("hasFollowUp", false);
            NoFurtherAction();
            return;
        }

        slate.Set("collectionTeam", collectionTeam);
        slate.Set("collector", collectionTeam[0]);

        string inSignalMakePawnsArrival = QuestGenUtility.HardcodedSignalWithQuestID("CollectionTeam_MakePawnsArrival");
        quest.Delay(delayTicks: GenMath.RoundTo(Rand.RangeInclusive(4 * 60000, 6 * 60000), 2500),
                    inner: null,
                    outSignalComplete: inSignalMakePawnsArrival);

        string inSignalPawnNegative = QuestGenUtility.HardcodedSignalWithQuestID("CollectionTeam_Negative");
        QuestPart_PawnNegativeSiganl questPart_PawnNegativeSiganl = new()
        {
            negativeSiganls = OAFrame_QuestUtility.GetCommonPawnNegativeSiganls(addTag: true, tagToAdd: "collectionTeam"),
            outOnlyOnce = false,
            outSignal = inSignalPawnNegative
        };
        quest.AddPart(questPart_PawnNegativeSiganl);

        QuestPart_CollectionTeam questPart_CollectionTeam = new()
        {
            inSignalEnable = inSignalMakePawnsArrival,
            InSignalMakePawnsLeave = inSignalPawnNegative,

            Faction = parentFaction,
            MapParent = map.Parent,

            DurationTicks = 60000,

            Pawns = [.. collectionTeam]
        };
        questPart_CollectionTeam.InitWithDefaultSignal();
        questPart_CollectionTeam.InitTalkTextRequest("[lostItemsOfTraderTalkText]");
        questPart_CollectionTeam.AddRequestThingDefCount(new ThingDefCountClass(ThingDefOf.Silver, Mathf.FloorToInt(silverCount * 0.8f)));
        quest.AddPart(questPart_CollectionTeam);

        quest.Letter(
            LetterDefOf.PositiveEvent,
            inSignal: inSignalMakePawnsArrival,
            label: "[lostItemsOfTraderArrivalLabel]",
            text: "[lostItemsOfTraderArrivalText]");

        string inSignalTeamForceEnd = QuestGenUtility.HardcodedSignalWithQuestID("CollectionTeam_ForceEnd");
        quest.Delay(
            delayTicks: 3 * 60000,
            inner: null,
            inSignalEnable: questPart_CollectionTeam.OutSignalPawnsArrived,
            outSignalComplete: inSignalTeamForceEnd);

        string inSignalTeamSuccess = QuestGenUtility.HardcodedSignalWithQuestID("CollectionTeam_Success");
        quest.SignalPassAll(inSignals: [questPart_CollectionTeam.OutSignalGive, inSignalTeamForceEnd], outSignal: inSignalTeamSuccess);
        quest.SignalPass(inSignal: questPart_CollectionTeam.OutSignalAllLeftMapAndGive, outSignal: inSignalTeamSuccess);

        QuestPart_AllOrdersEsteemChange questPart_AllOrdersEsteemChange_Success = new()
        {
            InSignalTrigger = inSignalTeamSuccess,
            Change = 1,
            ShowPlayerChangeMessage = true,
            Reason = "OARO_LostItemsOfTrader_Success".Translate()
        };
        quest.AddPart(questPart_AllOrdersEsteemChange_Success);
        QuestPart_OrderRecommendation questPart_OrderRecommendation_Success = new()
        {
            InSignalTrigger = inSignalTeamSuccess,
            Count = 1
        };
        quest.AddPart(questPart_OrderRecommendation_Success);
        quest.End(outcome: QuestEndOutcome.Success, 50, parentFaction, inSignal: inSignalTeamSuccess, sendStandardLetter: true, playSound: true);

        QuestPart_AllOrdersEsteemChange questPart_AllOrdersEsteemChange_Fail = new()
        {
            InSignalTrigger = questPart_CollectionTeam.OutSignalFailureToCollect,
            Change = -5,
            ShowPlayerChangeMessage = true,
            Reason = "OARO_LostItemsOfTrader_Fail".Translate()
        };
        quest.AddPart(questPart_AllOrdersEsteemChange_Fail);
        quest.End(outcome: QuestEndOutcome.Fail, -50, parentFaction, inSignal: questPart_CollectionTeam.OutSignalFailureToCollect, sendStandardLetter: true, playSound: true);

        quest.SignalPassActivable(
            action: delegate
            {
                QuestGen_End.End(quest, outcome: QuestEndOutcome.Unknown);
            },
            inSignal: inSignalTeamForceEnd,
            inSignalDisable: questPart_CollectionTeam.OutSignalDecided);
    }
}