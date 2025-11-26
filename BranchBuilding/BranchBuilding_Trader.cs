using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuilding_Trader : BranchBuildingWithComps, ITickDay
{
    protected OberoniaAurea_Frame.SiteTrader trader;
    public OberoniaAurea_Frame.SiteTrader Trader => trader;

    public void TickDay()
    {
        trader?.TickInterval(60000);
    }

    public override void InitActive()
    {
        base.InitActive();
        InitInnerTrader();
    }

    private void InitInnerTrader()
    {
        BranchBuildingTrderSetter_Extension trderSetter = def.GetModExtension<BranchBuildingTrderSetter_Extension>();
        if (trderSetter is null)
        {
            Log.Error($"[OARO] Missing BranchBuildingTrderSetter_Extension mod extension on def {def.defName}");
            return;
        }
        TraderKindDef traderKindDef = trderSetter.potentialTraders?.RandomElementWithFallback();
        if (traderKindDef is null)
        {
            Log.Error($"[OARO] No valid TraderKindDef found in potentialTraders for def {def.defName}");
            return;
        }

        int refreshInterval = trderSetter.refreshIntervalDays > 0 ? trderSetter.refreshIntervalDays * 60000 : -1;
        trader = new(traderKindDef, branch.BaseSite, branch.RatkinOrder.Faction, refreshInterval: refreshInterval);
        trader?.GenerateThings();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref trader, "trader");

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (trader is null)
            {
                InitInnerTrader();
            }
        }
    }
}