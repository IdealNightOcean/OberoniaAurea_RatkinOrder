using RimWorld;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderInteractionWorker_InviteResidentKnight(OrderInteractionDef def) : OrderInteractionWorker(def)
{
    /// <summary>
    /// 招募常驻骑士所需推荐信数量
    /// </summary>
    public static int GetRecommendationNeedCount(RatkinOrder ratkinOrder)
    {
        return ratkinOrder.Esteem switch
        {
            > 50 => 3,
            _ => 2
        };
    }

    public override AcceptanceReport CanUseInteraction(RatkinOrder ratkinOrder, Map map, bool resultOnly)
    {
        if (OrderHallHandler.Instance.OrderHallRoom is null)
        {
            return resultOnly ? false : "OARO_NoRatkinOrderHall".Translate();
        }

        int residentKnightCeiling = ResidentKnightsManager.ResidentKnightCeiling;
        if (ResidentKnightsManager.Instance.KnightsCount >= residentKnightCeiling)
        {
            return resultOnly ? false : "OARO_ReachMax_ResidentKnights".Translate(residentKnightCeiling);
        }

        AcceptanceReport baseAcceptance = base.CanUseInteraction(ratkinOrder, map, resultOnly);
        if (!baseAcceptance)
        {
            return baseAcceptance;
        }

        int recommendationNeed = GetRecommendationNeedCount(ratkinOrder);
        if (RecommendationUtility.CurRecommendationOfMap(ratkinOrder, map) < recommendationNeed)
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(recommendationNeed, ratkinOrder.Name);
        }

        return true;
    }

    protected override void ApplyInteraction(RatkinOrder ratkinOrder, Map map)
    {
        int recommendationNeed = GetRecommendationNeedCount(ratkinOrder);
        Dialog_NodeTreeWithRatkinOrderInfo nodeTree = OARO_WindowUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(
            text: "OARO_InviteResidentKnight_Confirm".Translate(recommendationNeed.ToString().Named(KeyLibrary_FormatArgName.Count)),
            ratkinOrder: ratkinOrder,
            acceptAction: () => base.ApplyInteraction(ratkinOrder, map));

        Find.WindowStack.Add(nodeTree);
    }

    protected override (bool succeeded, bool doPostApply) InteractionEffect(RatkinOrder ratkinOrder, Map map)
    {
        Branch branch = ratkinOrder.BranchManager.AllBranches.Where(b => b.IsBranchOfType(Branch.BranchType.Friendly)).RandomElementWithFallback(null);
        branch ??= ratkinOrder.BranchManager.AllBranches.Where(b => b.IsBranchOfType(Branch.BranchType.Honor)).RandomElementWithFallback(null);
        branch ??= ratkinOrder.BranchManager.AllBranches.RandomElementWithFallback(null);

        if (!branch.IsValid())
        {
            return (false, true);
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
            return (false, true);
        }

        ResidentKnightsManager.Instance.AddResidentKnight(knight, knightRecord);
        return (true, true);

        /*
        Slate slate = new();
        slate.SetBasicOrderSlateVar(ratkinOrder);
        slate.Set("map", map);

        if (OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, OARO_QuestScriptDefOf.OARO_Quest_ResidentKnight, slate, forced: false))
        {
            int recommendationNeed = GetRecommendationNeedCount(ratkinOrder);
            if (recommendationNeed > 0)
            {
                RecommendationUtility.UseRecommendationOfMap(ratkinOrder, map, recommendationNeed);
            }

            return (true, true);
        }
        return (false, true);
        */
    }
}