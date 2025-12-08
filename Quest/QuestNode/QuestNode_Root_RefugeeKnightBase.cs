using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class QuestNode_Root_RefugeeKnightBase : QuestNode_Root_RefugeeBase
{
    protected const string PawnGenGroupTag = "Options";

    protected override PawnKindDef FixedPawnKind => null;
    protected virtual PawnGroupKindDef PawnGroupKind => OARO_PawnGroupKindDefOf.OARO_KnightRefugee;

    protected virtual bool IsCombatant => false;
    protected virtual bool IsCommander => false;

    protected RatkinOrder ratkinOrder;
    protected Branch branch;

    protected bool InitRatkinOrder(bool initBranch)
    {
        Quest quest = QuestGen.quest;
        if (initBranch)
        {
            branch = QuestGen.slate.Get<Branch>(KeyLibrary_SlateStoreAs.Branch);
            if (!branch.IsValid())
            {
                return false;
            }

            QuestPart_InvolvedRatkinOrders.AddInvolvedRatkinOrder(quest, ratkinOrder);
            QuestPart_CriticalBranch questPart_CriticalBranch = new()
            {
                Branch = branch,
                EndQuest = true,
                EndOutcome = QuestEndOutcome.Fail
            };
            quest.AddPart(questPart_CriticalBranch);
        }
        ratkinOrder = QuestGen.slate.Get<RatkinOrder>(KeyLibrary_SlateStoreAs.RatkinOrder) ?? branch?.RatkinOrder;
        if (!ratkinOrder.IsValid())
        {
            return false;
        }
        QuestPart_InvolvedRatkinOrders.AddInvolvedRatkinOrder(quest, ratkinOrder);
        QuestPart_CriticalRatkinOrder questPart_CriticalRatkinOrder = new()
        {
            RatkinOrder = ratkinOrder,
            EndQuest = true,
            EndOutcome = QuestEndOutcome.Fail
        };
        quest.AddPart(questPart_CriticalRatkinOrder);
        return true;
    }

    protected override void ClearQuestParameter()
    {
        base.ClearQuestParameter();
        ratkinOrder = null;
        branch = null;
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

        PawnKindDef fixedPawnKind = FixedPawnKind;
        IReadOnlyList<PawnGenOption> pawnGenOptions = null;
        if (fixedPawnKind is null)
        {
            KnightGenerateUtility.TryGetRandomPawnGroupMakerForOrder(ratkinOrder, branch, PawnGroupKind, out PawnGroupOption pawnGroupOption);
            if (pawnGroupOption is null)
            {
                Log.Error($"[OARO] No usable {nameof(PawnGroupOption)} for {PawnGroupKind} found in {ratkinOrder}");
            }
            else
            {
                pawnGenOptions = pawnGroupOption.GetRandomGroupOptionsWithTag(PawnGenGroupTag);
            }

            if (pawnGenOptions is null || pawnGenOptions.Count == 0)
            {
                Log.Error($"[OARO] No usable {nameof(PawnGenOption)} with tag \"{PawnGenGroupTag}\" for select {nameof(PawnGroupOption)}");
                fixedPawnKind = OARO_PawnKindDefOf.RatkinKnight;
            }
        }

        int adultCount = questParameter.LodgerCount - questParameter.ChildCount;

        bool isCombatant = IsCombatant;
        bool isCommander = IsCommander;
        for (int i = 0; i < questParameter.LodgerCount; i++)
        {
            PawnKindDef pawnKind = fixedPawnKind ?? pawnGenOptions.RandomElementByWeight(g => g.selectionWeight).kind;
            PawnGenerationRequest generationRequest = KnightGenerateUtility.DefaultKnightGenerationRequest(pawnKind, questParameter.faction, tile: questParameter.map.Tile, forceNew: true);
            generationRequest.AllowedDevelopmentalStages = (i < adultCount ? DevelopmentalStage.Adult : DevelopmentalStage.Child);

            Pawn pawn = quest.GeneratePawn(generationRequest);
            KnightGenerateUtility.PostKnightGenerate(pawn, new KnightRecord(ratkinOrder, branch, isCombatant: isCombatant, isCommander: isCommander));

            pawns.Add(pawn);

            PostPawnGenerated(pawn, lodgerRecruitedSignal);
        }
        return pawns;
    }
}