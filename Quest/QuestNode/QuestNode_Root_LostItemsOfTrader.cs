using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 旅行商的失物
/// </summary>
public class QuestNode_Root_LostItemsOfTrader : QuestNode
{
    public static readonly IntRange SilverCountRange = new(5000, 8000);

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        Quest quest = QuestGen.quest;

        Map map = slate.Get<Map>("map") ?? OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
        if (map is null)
        {
            quest.End(QuestEndOutcome.Unknown, inSignal: null);
            return;
        }

        slate.Set("map", map);

        Faction parentFaction = slate.Get<Faction>(KeyLibrary_SlateStoreAs.ParentFaction);
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
        if (slate.TryGet(KeyLibrary_SlateStoreAs.HelpSeeker, out Pawn helpSeeker))
        {
            spawnCell = helpSeeker.PositionHeld;
        }
        int silverCount = SilverCountRange.RandomInRange;
        Thing silverLost = ThingMaker.MakeThing(ThingDefOf.Silver);
        silverLost.stackCount = silverCount;
        quest.SpawnThing(map, silverLost, cell: spawnCell, lookForSafeSpot: true, questLookTarget: false);

        if (parentFaction is null || Rand.Chance(0.5f))
        {
            NoFurtherAction();
        }
        else
        {
            FollowUpAction(map, parentFaction, silverCount);
        }
    }

    private void NoFurtherAction()
    {
        QuestGen.quest.Delay(
            delayTicks: Rand.RangeInclusive(4, 6) * 60000,
            inner: delegate
            {
                QuestGen.quest.Letter(letterDef: LetterDefOf.PositiveEvent,
                                      text: "OARO_LostItemsOfTrader_NoFurtherText".Translate(),
                                      label: "OARO_LostItemsOfTrader_NoFurtherLabel".Translate());
                QuestGen_End.End(QuestGen.quest, outcome: QuestEndOutcome.Success);
            });
    }

    private void FollowUpAction(Map map, Faction parentFaction, int silverCount)
    {
        Slate slate = QuestGen.slate;
        Quest quest = QuestGen.quest;

        string inSignalMakePawnsArrival = QuestGenUtility.HardcodedSignalWithQuestID("CollectionTeam_MakePawnsArrival");
        quest.Delay(delayTicks: GenMath.RoundTo(Rand.RangeInclusive(4 * 60000, 6 * 60000), 2500), inner: null, outSignalComplete: inSignalMakePawnsArrival);

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
            Faction = parentFaction,
            MapParent = map.Parent,
            PawnGroupMakerDef = OARO_ModDefOf.OARO_LostItemsOfTrader,

            DurationTicks = 60000,
            TalkText = "OARO_Talk_LostItemsOfTrader".Translate()
        };
        questPart_CollectionTeam.InitWithDefaultSignal();
        questPart_CollectionTeam.inSignalEnable = inSignalMakePawnsArrival;
        questPart_CollectionTeam.InSignalMakePawnsLeave = inSignalPawnNegative;
        questPart_CollectionTeam.AddRequestThingDefCount(new ThingDefCountClass(ThingDefOf.Silver, Mathf.FloorToInt(silverCount * 0.8f)));
        quest.AddPart(questPart_CollectionTeam);

        string inSignalTeamForceEnd = QuestGenUtility.HardcodedSignalWithQuestID("CollectionTeam_ForceEnd");
        quest.Delay(delayTicks: 180000, inner: null, inSignalEnable: questPart_CollectionTeam.OutSignalPawnsArrived, outSignalComplete: inSignalTeamForceEnd);

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
        quest.End(outcome: QuestEndOutcome.Fail, 50, parentFaction, inSignal: inSignalTeamSuccess, sendStandardLetter: true, playSound: true);

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