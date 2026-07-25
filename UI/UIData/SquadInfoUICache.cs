using NightOcean;
using RimWorld;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.Branch;

namespace OberoniaAurea.RatkinOrder;

public class SquadInfoUICache : BranchSummaryUICache
{
    public float FriendlyProcess { get; }
    public string FriendlyExpireDateStr { get; } = string.Empty;

    public int CommanderCeiling { get; } = -1;
    public float MemberRecoveryRate { get; } = -1f;

    public LazyMutable<string> MemberRecoveryRateExplanation { get; }

    public SquadInfoUICache() : base()
    {
        MemberRecoveryRateExplanation = new(refreshFunc: () => string.Empty);
    }

    public SquadInfoUICache(Branch branch, Map map) : base(branch, map)
    {
        MemberRecoveryRateExplanation = new(refreshFunc: () => BranchStatDefOf.OARO_SquadMemberRecoveryRate.GetStatModifyExplanation(new BranchStatRequestData(branch)).explanation);

        if (branch.IsBranchOfType(BranchType.Friendly))
        {
            FriendlyProcess = Mathf.Clamp01(branch.FriendlyDaysLeft / (float)BranchUtility.GetDefaultFriendlyDurationDays(branch));
            FriendlyExpireDateStr = GenDate.SeasonDateStringAt(GenTicks.TicksAbs + branch.FriendlyDaysLeft, Find.WorldGrid.LongLatOf(map.Tile));
        }

        CommanderCeiling = (int)branch.Squad.CommanderCeiling;
        MemberRecoveryRate = branch.GetStatValue(BranchStatDefOf.OARO_SquadMemberRecoveryRate, immediateUpdate: true);
    }

    public void ClearCache()
    {
        MemberRecoveryRateExplanation.Reset();
    }
}