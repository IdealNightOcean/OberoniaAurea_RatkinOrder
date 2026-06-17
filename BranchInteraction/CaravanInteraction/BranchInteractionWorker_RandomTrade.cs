using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionWorker_RandomTrade(BranchInteractionDef def) : BranchInteractionWorker_CaravanOnly(def)
{
    protected override void ApplyCost(BranchInteractionParms parms)
    {
        base.ApplyCost(parms);
        parms.Branch.CooldownManager.RegisterRecord(Def.defName, cdTicks: 600, removeWhenExpired: true);
    }

    /// <returns>
    /// <para>- doPostApply：始终返回 <see langword="false"/> 以阻止 <see cref="ApplyInteraction"/> 执行回调方法 <see cref="BranchInteractionWorker.PostApplyEffect"/></para>
    /// </returns>
    protected override (bool succeeded, bool doPostApply) InteractionEffect(BranchInteractionParms parms)
    {
        Pawn negotiator = BestCaravanPawnUtility.FindBestNegotiator(parms.TargetCaravan);
        if (negotiator is null || negotiator.skills.GetSkill(SkillDefOf.Social).TotallyDisabled)
        {
            Messages.Message("OAFrame_MessageNoTrader".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            return (false, false);
        }

        Faction faction = OAFrame_FactionUtility.RandomAvailableFactionOf(FactionValidationParams.DefaultFaction, HasTrader);
        if (faction is null)
            return (false, false);

        TraderKindDef traderKind = AllTraderKinds(faction.def).RandomElementWithFallback(null);
        if (traderKind is null)
            return (false, false);

        SiteTrader trader = new(traderKind, parms.Branch.BaseSite, faction, refreshInterval: -1);
        if (trader is null)
            return (false, false);

        trader.GenerateThings();

        Dialog_BranchTrade_SingleUse branchTrade = new(negotiator, trader);
        branchTrade.InitForInteraction(parms);
        branchTrade.PostApplyBranchInteraction += PostApplyEffect;
        branchTrade.PostApplyBranchInteraction += OnDestroyTrader;

        Find.WindowStack.Add(branchTrade);

        return (true, false);

        void OnDestroyTrader(BranchInteractionParms _, bool __)
        {
            trader?.Destroy();
        }

        static bool HasTrader(Faction argFaction)
        {
            FactionDef argFactionDef = argFaction.def;
            return !(argFactionDef.baseTraderKinds.NullOrEmpty() && argFactionDef.caravanTraderKinds.NullOrEmpty() && argFactionDef.orbitalTraderKinds.NullOrEmpty() && argFactionDef.visitorTraderKinds.NullOrEmpty());
        }

        static IEnumerable<TraderKindDef> AllTraderKinds(FactionDef argFactionDef)
        {
            if (!argFactionDef.baseTraderKinds.NullOrEmpty())
            {
                foreach (TraderKindDef t in argFactionDef.baseTraderKinds)
                {
                    yield return t;
                }
            }
            if (!argFactionDef.caravanTraderKinds.NullOrEmpty())
            {
                foreach (TraderKindDef t in argFactionDef.caravanTraderKinds)
                {
                    yield return t;
                }
            }
            if (!argFactionDef.orbitalTraderKinds.NullOrEmpty())
            {
                foreach (TraderKindDef t in argFactionDef.orbitalTraderKinds)
                {
                    yield return t;
                }
            }
            if (!argFactionDef.visitorTraderKinds.NullOrEmpty())
            {
                foreach (TraderKindDef t in argFactionDef.visitorTraderKinds)
                {
                    yield return t;
                }
            }
        }
    }
}