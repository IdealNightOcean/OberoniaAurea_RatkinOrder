using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionWorker_BuildingTrader(BranchInteractionDef def) : BranchInteractionWorker_CaravanOnly(def)
{
    protected override AcceptanceReport ParmsValidate(BranchInteractionParms parms, bool resultOnly)
    {
        if (parms.Building is not BranchBuilding_Trader)
        {
            return resultOnly ? false : "OARO_NotTraderContainerBranchBuilding".Translate();
        }
        return base.ParmsValidate(parms, resultOnly);
    }

    /// <returns>
    /// <para>- doPostApply：始终返回 <see langword="false"/> 以阻止 <see cref="BranchInteractionWorker.ApplyInteraction"/> 执行回调方法 <see cref="BranchInteractionWorker.PostApplyInteraction"/></para>
    /// </returns>
    protected override (bool succeeded, bool doPostApply) InteractionEffect(BranchInteractionParms parms)
    {
        BranchBuilding building = parms.Building;
        if (building is not BranchBuilding_Trader traderContainer || traderContainer.Trader is null)
        {
            Log.Error($"[OARO] Failed to apply BranchInteraction: {nameof(building)} is not a {nameof(BranchBuilding_Trader)} or its {nameof(BranchBuilding_Trader.Trader)} is null. {nameof(BranchBuildingDef)}: {building?.Def?.defName ?? "Unknown"}");
            return (false, false);
        }

        SiteTrader trader = traderContainer.Trader;
        Pawn negotiator = BestCaravanPawnUtility.FindBestNegotiator(parms.Caravan, trader.Faction, trader.TraderKind);
        if (negotiator is null || negotiator.skills.GetSkill(SkillDefOf.Social).TotallyDisabled)
        {
            Messages.Message("OAFrame_MessageNoTrader".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            return (false, false);
        }

        Dialog_BranchTrade branchTrade = new(negotiator, trader);
        branchTrade.InitForInteraction(parms);
        branchTrade.PostApplyBranchInteraction += PostApplyInteraction;

        Find.WindowStack.Add(branchTrade);

        return (true, false);
    }
}