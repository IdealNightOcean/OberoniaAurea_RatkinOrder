using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_Root_InDistressKnight : QuestNode_Root_RefugeeKnightBase
{
    protected override PawnKindDef FixedPawnKind => OARO_PawnKindDefOf.RatkinKnight;

    protected override bool IsCombatant => true;
    protected override bool IsCommander => true;

    private string InSignalRecruited { get; set; }

    protected override bool TestRunInt(Slate slate)
    {
        return RatkinOrderManager.Instance.AllRatkinOrders.Count > 0;
    }

    protected override bool InitQuestParameter()
    {
        questParameter = new()
        {
            allowAssaultColony = false,
            allowBadThought = true,
            allowLeave = true,

            allowFutureReward = false,
            allowJoinOffer = false,

            LodgerCount = 1,
            ChildCount = 0,

            questDurationTicks = (5 + 7) * 60000
        };

        InSignalRecruited = QuestGenUtility.HardcodedSignalWithQuestID("Lodgers_Recruited");
        QuestGen.slate.Set(UniqueQuestDescSlate, true);
        QuestGen.slate.Set(UniqueLeavingLetterSlate, true);

        return InitRatkinOrder(initBranch: true);
    }

    protected override void ClearQuestParameter()
    {
        base.ClearQuestParameter();
        InSignalRecruited = null;
    }

    protected override bool InitRatkinOrder(bool initBranch)
    {
        PlanetTile tile = questParameter.map.Tile;
        Branch targetBranch = null;
        float minDistance = float.MaxValue;

        foreach (RatkinOrder ratkinOrder in RatkinOrderManager.Instance.AllRatkinOrders)
        {
            foreach (Branch branch in ratkinOrder.BranchManager.AllBranches)
            {
                float distance = branch.DistanceTo(tile);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    targetBranch = branch;
                }
            }
        }

        if (!targetBranch.IsValid())
        {
            targetBranch = RatkinOrderManager.Instance.AllRatkinOrders.RandomElementWithFallback(fallback: null)?.BranchManager.AllBranches.RandomElementWithFallback(fallback: null);
        }

        if (!targetBranch.IsValid())
        {
            return false;
        }

        Branch = targetBranch;
        QuestGen.slate.Set(KeyLibrary_SlateStoreAs.branch, Branch);
        RatkinOrder = targetBranch.RatkinOrder;
        QuestGen.slate.Set(KeyLibrary_SlateStoreAs.ratkinOrder, RatkinOrder);

        return base.InitRatkinOrder(initBranch);
    }

    protected override void PostPawnGenerated(Pawn pawn, string lodgerRecruitedSignal)
    {
        base.PostPawnGenerated(pawn, lodgerRecruitedSignal);
        OAFrame_PawnUtility.TakeNonLethalDamage(pawn, injuriesCount: 6, fixedDamageDef: DamageDefOf.Blunt);
        pawn.health.AddHediff(OARO_HediffDefOf.OARO_Hediff_InDistressKnight);
    }

    protected override void PawnArrival(string lodgerArrivalSignal)
    {
        Quest quest = QuestGen.quest;
        string inSignalHelpAccepted = QuestGenUtility.HardcodedSignalWithQuestID("Lodgers_HelpAccepted");
        string inSignalHelpRejected = QuestGenUtility.HardcodedSignalWithQuestID("Lodgers_HelpRejected");

        QuestPart_InDistressKnightStartLetter questPart_InDistressKnightStartLetter = new()
        {
            InSignal = QuestGen.slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal),
            RelatedOrder = RatkinOrder,
            RelatedFaction = RatkinOrder.Faction,
            OutSignalAccepted = inSignalHelpAccepted,
            OutSignalRejected = inSignalHelpRejected,
            LetterDef = OARO_LetterDefOf.OARO_Order_InDistressKnightStartLetter
        };
        questPart_InDistressKnightStartLetter.InitLetterTextRequest("[inDistressKnightStartLetterLabel]", "[inDistressKnightStartLetterText]");
        quest.AddPart(questPart_InDistressKnightStartLetter);

        quest.Signal(inSignal: inSignalHelpAccepted, action: delegate
        {
            quest.PawnsArrive(pawns: questParameter.pawns,
                              mapParent: questParameter.map.Parent,
                              joinPlayer: true,
                              customLetterLabel: "[lodgersArriveLetterLabel]",
                              customLetterText: "[lodgersArriveLetterText]");

            quest.SendSignals([lodgerArrivalSignal]);
        });

        quest.Delay(delayTicks: 3 * 60000, inner: delegate
        {
            quest.Letter(letterDef: LetterDefOf.NeutralEvent,
                         text: "[helpStageIILetterText]",
                         label: "[helpStageIILetterLabel]",
                         relatedFaction: questParameter.faction);
        });

        quest.Delay(delayTicks: 4 * 60000, inner: delegate
        {
            QuestPart_OrderLetter questPart_OrderLetter = new()
            {
                InSignal = QuestGen.slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal),
                OrderLetterDef = OrderLetterDefOf.OARO_OfficialLetter_SimpleAttachments,
                RelatedLetterType = OrderLetter.RelatedLetterType.Positive,
                RelatedOrder = RatkinOrder,
                RelatedBranch = Branch,
            };
            questPart_OrderLetter.InitLetterTextRequest("[helpThankLetterLabel]", "[helpThankLetterText]", Branch.NameColored);
            List<Thing> rewards = OAFrame_MiscUtility.TryGenerateThing(ThingDefOf.Silver, 500);
            OrderRecommendation recommendation = (OrderRecommendation)ThingMaker.MakeThing(OARO_ThingDefOf.OARO_OrderRecommendation);
            recommendation.SetRatkinOrder(RatkinOrder);
            rewards.Add(recommendation);
            questPart_OrderLetter.InitAttachments(rewards);
            QuestGen.quest.AddPart(questPart_OrderLetter);

            QuestPart_OrderLetter questPart_OrderLetter_GuidanceI = new()
            {
                InSignal = QuestGen.slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal),
                OrderLetterDef = OrderLetterDefOf.OARO_UrgentLetter,
                RelatedLetterType = OrderLetter.RelatedLetterType.Positive,
                RelatedOrder = RatkinOrder,
                RelatedBranch = Branch,
            };
            questPart_OrderLetter_GuidanceI.InitLetterTextRequest("[guidanceILetterLabel]", "[guidanceILetterText]", Branch.NameColored);
            QuestGen.quest.AddPart(questPart_OrderLetter_GuidanceI);

            QuestPart_OrderLetter questPart_OrderLetter_GuidanceII = new()
            {
                InSignal = QuestGen.slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal),
                OrderLetterDef = OrderLetterDefOf.OARO_UrgentLetter,
                RelatedLetterType = OrderLetter.RelatedLetterType.Positive,
                RelatedOrder = RatkinOrder,
                RelatedBranch = Branch,
            };
            questPart_OrderLetter_GuidanceI.InitLetterTextRequest("[guidanceIILetterLabel]", "[guidanceIILetterText]", Branch.NameColored);
            QuestGen.quest.AddPart(questPart_OrderLetter_GuidanceII);

            QuestPart_OrderLetter questPart_OrderLetter_GuidanceIII = new()
            {
                InSignal = QuestGen.slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal),
                OrderLetterDef = OrderLetterDefOf.OARO_UrgentLetter,
                RelatedLetterType = OrderLetter.RelatedLetterType.Positive,
                RelatedOrder = RatkinOrder,
                RelatedBranch = Branch,
            };
            questPart_OrderLetter_GuidanceI.InitLetterTextRequest("[guidanceIIILetterLabel]", "[guidanceIIILetterText]", Branch.NameColored);
            QuestGen.quest.AddPart(questPart_OrderLetter_GuidanceIII);

            QuestPart_OrderLetter questPart_OrderLetter_GuidanceIV = new()
            {
                InSignal = QuestGen.slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal),
                OrderLetterDef = OrderLetterDefOf.OARO_UrgentLetter,
                RelatedLetterType = OrderLetter.RelatedLetterType.Positive,
                RelatedOrder = RatkinOrder,
                RelatedBranch = Branch,
            };
            questPart_OrderLetter_GuidanceI.InitLetterTextRequest("[guidanceIVLetterLabel]", "[guidanceIVLetterText]", Branch.NameColored);
            QuestGen.quest.AddPart(questPart_OrderLetter_GuidanceIV);
        });

        quest.SignalPassActivable(action: delegate
        {
            quest.Letter(
                letterDef: LetterDefOf.NeutralEvent,
                text: "[helpRejectedLetterText]",
                label: "[helpRejectedLetterLabel]",
                relatedFaction: questParameter.faction
                );
            QuestGen_End.End(quest, QuestEndOutcome.Unknown);
        },
        inSignal: inSignalHelpRejected,
        inSignalDisable: inSignalHelpAccepted);
    }

    protected override void SetPawnsLeaveComp(string lodgerArrivalSignal, string inSignalRemovePawn)
    {
        Quest quest = QuestGen.quest;

        string inSignalMakeLeaved = QuestGenUtility.HardcodedSignalWithQuestID("Lodgers_MakeLeaved");
        quest.Delay(
            delayTicks: 7 * 60000,
            inner: delegate
            {
                QuestPart_InDistressKnightLeaveLetter questPart_InDistressKnightLeaveLetter = new()
                {
                    InSignal = QuestGen.slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal),
                    InSignalRemovePawn = inSignalRemovePawn,
                    OutSignalMakeLeave = inSignalMakeLeaved,
                    OutSignalRecruit = InSignalRecruited,

                    LetterDef = OARO_LetterDefOf.OARO_Order_InDistressKnightLeaveLetter,
                    LookTargets = new LookTargets(questParameter.pawns),
                    RelatedOrder = RatkinOrder,
                    Pawns = []
                };
                questPart_InDistressKnightLeaveLetter.Pawns.AddRange(questParameter.pawns);
                questPart_InDistressKnightLeaveLetter.InitLetterTextRequest("[helpThanckLeaveQuizLetterLabel]", "[helpThanckLeaveQuizLetterText]");
                QuestGen.quest.AddPart(questPart_InDistressKnightLeaveLetter);
            },
            isQuestTimeout: false,
            inSignalEnable: "GuestsDepartsIn".Translate(),
            inSignalDisable: "GuestsDepartsOn".Translate(),
            outSignalComplete: "QuestDelay");

        quest.SignalPassAny(
            action: delegate
            {
                QuestPart_OrderLetter questPart_OrderLetter = new()
                {
                    InSignal = QuestGen.slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal),
                    RelatedOrder = RatkinOrder,
                    RelatedBranch = Branch,
                    RelatedLetterType = OrderLetter.RelatedLetterType.Positive,

                    DelayDays = 1
                };
                questPart_OrderLetter.InitLetterTextRequest("[helpThankFinalLetterLabel]", "[helpThankFinalLetterText]", Branch.NameColored);
                QuestGen.quest.AddPart(questPart_OrderLetter);
            },
            inSignals: [inSignalMakeLeaved, InSignalRecruited]);


        quest.SignalPassActivable(
            inSignal: inSignalMakeLeaved,
            action: delegate
            {
                quest.SignalPassWithFaction(questParameter.faction, null, delegate
                {
                    quest.Letter(letterDef: LetterDefOf.PositiveEvent,
                                 filterDeadPawnsFromLookTargets: false,
                                 text: "[lodgersLeavingLetterText]",
                                 label: "[lodgersLeavingLetterLabel]");
                });
                quest.Leave(pawns: questParameter.pawns,
                            sendStandardLetter: false,
                            leaveOnCleanup: false,
                            inSignalRemovePawn: inSignalRemovePawn,
                            wakeUp: true);
            },
            inSignalEnable: lodgerArrivalSignal,
            inSignalDisable: InSignalRecruited);
    }

    protected override void SetQuestEndComp(QuestPart_OARefugeeInteractions questPart_Interactions, string failSignal, string delayFailSignal, string successSignal)
    {
        base.SetQuestEndComp(questPart_Interactions, failSignal, delayFailSignal, successSignal);
        QuestGen.quest.End(QuestEndOutcome.Success, inSignal: InSignalRecruited);
    }
}