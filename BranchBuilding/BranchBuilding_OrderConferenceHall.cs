using RimWorld;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuilding_OrderConferenceHall : BranchBuilding, ITickDay
{
    public void TickDay()
    {
        if (branch.RatkinOrder.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.OrderConferenceNegotiate))
        {
            return;
        }

        if (branch.RatkinOrder.Faction.HostileTo(Faction.OfPlayer))
        {
            return;
        }

        bool hasAffectedHomeMap = false;
        foreach (Map map in Find.Maps.Where(m => m.IsPlayerHome))
        {
            if (branch.IsInAffectedRange(map.Tile))
            {
                hasAffectedHomeMap = true;
                break;
            }
        }

        if (!hasAffectedHomeMap)
        {
            return;
        }

        foreach (Faction faction in Find.FactionManager.AllFactions)
        {
            if (faction.HasGoodwill && !faction.IsPlayerSafe())
            {
                int upgrade = hasUpgraded ? 12 : 7;
                faction.TryAffectGoodwillWith(Faction.OfPlayer, upgrade, lookTarget: branch.BaseSite, reason: OARO_ModDefOf.OARK_OrderConferenceHall);
            }
        }

        branch.RatkinOrder.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.OrderConferenceNegotiate, cdTicks: 20 * 60000, removeWhenExpired: true);

    }
}