using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class QuestNode_Root_RefugeeKnightBase : QuestNode_Root_RefugeeBase
{
    protected RatkinOrder ratkinOrder;

    protected void InitRatkinOrder()
    {
        ratkinOrder = QuestGen.slate.Get<RatkinOrder>(KeyLibrary_SlateStoreAs.RatkinOrder);
        QuestPart_InvolvedRatkinOrders.AddInvolvedRatkinOrder(QuestGen.quest, ratkinOrder);
        QuestPart_CriticalRatkinOrder questPart_CriticalRatkinOrder = new()
        {
            RatkinOrder = ratkinOrder,
            EndQuest = true,
            EndOutcome = QuestEndOutcome.Fail
        };
        QuestGen.quest.AddPart(questPart_CriticalRatkinOrder);
    }

    protected override void ClearQuestParameter()
    {
        base.ClearQuestParameter();
        ratkinOrder = null;
    }

    protected override Faction GetOrGenerateFaction()
    {
        QuestGen.slate.Set(IsMainFactionSlate, true);
        return QuestGen.slate.Get<Faction>(KeyLibrary_SlateStoreAs.OrderFaction);
    }

    protected override List<Pawn> GeneratePawns(string lodgerRecruitedSignal = null)
    {
        Quest quest = QuestGen.quest;
        List<Pawn> pawns = [];
        int adultCount = questParameter.LodgerCount - questParameter.ChildCount;

        PawnKindDef fixedPawnKind = FixedPawnKind ?? PawnKindDefOf.Refugee;
        ThoughtDef thoughtToAdd = ThoughtToAdd;
        for (int i = 0; i < questParameter.LodgerCount; i++)
        {
            DevelopmentalStage developmentalStages = i < adultCount ? DevelopmentalStage.Adult : DevelopmentalStage.Child;
            PawnGenerationRequest generationRequest = OARO_PawnUtility.DefaultKnightGenerationRequest(fixedPawnKind, questParameter.faction, tile: questParameter.map.Tile, forceNew: true);
            generationRequest.AllowedDevelopmentalStages = developmentalStages;


            Pawn pawn = OARO_PawnUtility.GenerateOrderKnight(generationRequest, new KnightRecord(ratkinOrder));
            if (!pawn.IsWorldPawn())
            {
                Find.WorldPawns.PassToWorld(pawn);
            }

            pawns.Add(pawn);

            PostPawnGenerated(pawn);
            if (thoughtToAdd is not null)
            {
                QuestPart_AddMemoryThought questPart_AddMemoryThought = new()
                {
                    inSignal = QuestGen.slate.Get<string>("inSignal"),
                    pawn = pawn,
                    def = thoughtToAdd
                };
                quest.AddPart(questPart_AddMemoryThought);
            }

            if (questParameter.allowJoinOffer)
            {
                quest.PawnJoinOffer(pawn,
                "LetterJoinOfferLabel".Translate(pawn.Named("PAWN")),
                "LetterJoinOfferTitle".Translate(pawn.Named("PAWN")),
                "LetterJoinOfferText".Translate(pawn.Named("PAWN"),
                questParameter.map.Parent.Named("MAP")),
                delegate
                {
                    quest.JoinPlayer(questParameter.map.Parent, Gen.YieldSingle(pawn), joinPlayer: true);
                    quest.Letter(LetterDefOf.PositiveEvent,
                                 inSignal: null,
                                 chosenPawnSignal: null,
                                 relatedFaction: null,
                                 useColonistsOnMap: null,
                                 useColonistsFromCaravanArg: false,
                                 QuestPart.SignalListenMode.OngoingOnly,
                                 lookTargets: null,
                                 filterDeadPawnsFromLookTargets: false,
                                 label: "LetterLabelMessageRecruitSuccess".Translate() + ": " + pawn.LabelShortCap,
                                 text: "MessageRecruitJoinOfferAccepted".Translate(pawn.Named("RECRUITEE")));
                    quest.SignalPass(null, null, lodgerRecruitedSignal);
                },
                delegate
                {
                    quest.RecordHistoryEvent(HistoryEventDefOf.CharityRefused_ThreatReward_Joiner);
                },
                inSignal: null, outSignalPawnAccepted: null, outSignalPawnRejected: null,
                charity: true);
            }
        }
        return pawns;
    }

    protected override void PostPawnGenerated(Pawn pawn)
    {
        pawn.InitKnightHediff(new KnightRecord(ratkinOrder));
    }
}
