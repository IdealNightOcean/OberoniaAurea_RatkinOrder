using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionWorker_BuildingTrader(BranchInteractionDef def) : BranchInteractionWorker(def)
{
    public override AcceptanceReport CanUseInteraction(Branch branch, Caravan caravan, BranchBuilding building = null, bool resultOnly = false)
    {
        if (building is not BranchBuilding_Trader)
        {
            return resultOnly ? false : "OARO_NotTraderContainerBranchBuilding".Translate();
        }
        return base.CanUseInteraction(branch, caravan, building, resultOnly);
    }

    /// <summary>始终返回 <see langword="false"/> 以阻止 <see cref="ApplyInteraction"/> 执行回调方法 <see cref="PostApplyInteraction"/></summary>
    /// <returns>始终返回 <see langword="false"/></returns>
    protected override bool InteractionEffect(InteractionParms parms)
    {
        BranchBuilding building = parms.Building;
        if (building is not BranchBuilding_Trader traderContainer || traderContainer.Trader is null)
        {
            Log.Error($"[OARO] Failed to apply BranchInteraction: {nameof(building)} is not a {nameof(BranchBuilding_Trader)} or its {nameof(BranchBuilding_Trader.Trader)} is null. {nameof(BranchBuildingDef)}: {building?.Def?.defName ?? "Unknown"}");
            return false;
        }

        SiteTrader trader = traderContainer.Trader;
        Pawn negotiator = BestCaravanPawnUtility.FindBestNegotiator(parms.Caravan, trader.Faction, trader.TraderKind);
        if (negotiator is null || !negotiator.skills.GetSkill(SkillDefOf.Social).TotallyDisabled)
        {
            Messages.Message("OAFrame_MessageNoTrader".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }

        Dialog_BranchTrade branchTrade = new(negotiator, trader);
        branchTrade.InitForInteraction(parms.Branch, parms.Caravan, parms.Building);
        branchTrade.PostApplyBranchInteraction += PostApplyInteraction;

        Find.WindowStack.Add(branchTrade);

        return false;
    }
}

public class Dialog_BranchTrade(Pawn playerNegotiator, ITrader trader, bool giftsOnly = false) : Dialog_Trade(playerNegotiator, trader, giftsOnly)
{
    private Branch branch;
    private Caravan caravan;
    private BranchBuilding building = null;

    public Action<Branch, Caravan, BranchBuilding> PostApplyBranchInteraction { get; set; }

    public void InitForInteraction(Branch branch, Caravan caravan, BranchBuilding building = null)
    {
        this.branch = branch;
        this.caravan = caravan;
        this.building = building;
    }

    public override void Close(bool doCloseSound = true)
    {
        PostApplyBranchInteraction?.Invoke(branch, caravan, building);
        PostApplyBranchInteraction = null;
        base.Close(doCloseSound);
    }
}