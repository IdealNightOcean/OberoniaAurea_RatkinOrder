using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderInteractionWorker_InviteResidentKnight(OrderInteractionDef def) : OrderInteractionWorker(def)
{

    public override AcceptanceReport CanUseInteraction(RatkinOrder ratkinOrder, Map map, bool resultOnly)
    {
        if (OrderStationHandler.Instance.OrderHallRoom is null)
        {
            return resultOnly ? false : "OARO_NoRatkinOrderHall".Translate();
        }

        int residentKnightCeiling = ResidentPawnsManager.ResidentKnightCeiling;
        if (ResidentPawnsManager.Instance.KnightsCount >= residentKnightCeiling)
        {
            return resultOnly ? false : "OARO_ReachMax_ResidentKnights".Translate(ResidentPawnsManager.Instance.KnightsCount, residentKnightCeiling);
        }

        AcceptanceReport baseAcceptance = base.CanUseInteraction(ratkinOrder, map, resultOnly);
        if (!baseAcceptance)
        {
            return baseAcceptance;
        }

        int recommendationNeed = RecommendationUtility.RecommendationNeed_RecruitmentKnight(ratkinOrder);
        if (RecommendationUtility.CurRecommendationCount(map) < recommendationNeed)
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(recommendationNeed.Named(KeyLibrary_FormatArgName.Count));
        }

        return true;
    }

    protected override void ApplyInteraction(RatkinOrder ratkinOrder, Map map)
    {
        int recommendationNeed = RecommendationUtility.RecommendationNeed_RecruitmentKnight(ratkinOrder);
        Dialog_NodeTreeWithRatkinOrderInfo nodeTree = OARO_WindowUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(
            text: "OARO_InviteResidentKnight_Confirm".Translate(recommendationNeed.Named(KeyLibrary_FormatArgName.Count)),
            ratkinOrder: ratkinOrder,
            acceptAction: () => base.ApplyInteraction(ratkinOrder, map));

        Find.WindowStack.Add(nodeTree);
    }

    protected override void DoInteractionCost(RatkinOrder ratkinOrder, Map map)
    {
        base.DoInteractionCost(ratkinOrder, map);
        int recommendationNeed = RecommendationUtility.RecommendationNeed_RecruitmentKnight(ratkinOrder);
        RecommendationUtility.UseRecommendationOfMap(map, recommendationNeed);
    }

    protected override (bool succeeded, bool doPostApply) InteractionEffect(RatkinOrder ratkinOrder, Map map)
    {
        Branch branch = ratkinOrder.BranchManager.GetAllBranchesOfType(Branch.BranchType.Friendly).RandomElementWithFallback(null);
        branch ??= ratkinOrder.BranchManager.GetAllBranchesOfType(Branch.BranchType.Honor).RandomElementWithFallback(null);
        branch ??= ratkinOrder.BranchManager.AllBranches.RandomElementWithFallback(null);

        if (!branch.IsValid())
        {
            return (false, false);
        }

        KnightRecord knightRecord = new(ratkinOrder, branch, isCombatant: true, isCommander: false);
        Pawn knight = KnightGenerateUtility.GenerateKnight(OARO_PawnKindDefOf.RatkinKnight, knightRecord, map.Tile);
        IncidentParms parms = new()
        {
            target = map,
            faction = ratkinOrder.Faction
        };
        if (!ModUtility.TryMakePawnArrival([knight], parms, PawnsArrivalModeDefOf.EdgeDrop, joinPlayer: true))
        {
            return (false, false);
        }

        ResidentPawnsManager.Instance.RegisterKnight(knight, knightRecord);
        return (true, true);
    }
}