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
    public int BombardSupportCeiling { get; } = -1;

    public AcceptanceReport SupportFeasibility { get; } = false;
    public AcceptanceReport BombardFeasibility { get; } = false;

    public LazyMutable<string> MemberRecoveryRateExplanation;

    public SquadInfoUICache() : base()
    {
        MemberRecoveryRateExplanation = new(refreshFunc: () => string.Empty);
    }

    public SquadInfoUICache(Branch branch, Map map) : base(branch, map)
    {
        MemberRecoveryRateExplanation = new(refreshFunc: () => BranchStatUtility.GetStatModifyExplanationSet(Branch, BranchStatDefOf.OARO_SquadMemberRecoveryRate, showResultValue: true));

        if (branch.IsBranchOfType(BranchType.Friendly))
        {
            FriendlyProcess = Mathf.Clamp01(branch.FriendlyDaysLeft / (float)BranchUtility.GetDefaultFriendlyDurationDays(branch));
            FriendlyExpireDateStr = GenDate.SeasonDateStringAt(GenTicks.TicksAbs + branch.FriendlyDaysLeft, Find.WorldGrid.LongLatOf(map.Tile));
        }

        BombardFeasibility = BranchSupportUtility.CanBombard(branch, map, resultOnly: false);
        SupportFeasibility = BranchSupportUtility.CanCombatKnightSupport(branch, map, BranchSupportUtility.DeploymentLevel.Quarter, resultOnly: false);

        CommanderCeiling = (int)branch.Squad.CommanderCeiling;
        MemberRecoveryRate = branch.GetStatValue(BranchStatDefOf.OARO_SquadMemberRecoveryRate);
        BombardSupportCeiling = (int)branch.GetStatValue(BranchStatDefOf.OARO_BombardSupportCeiling);
    }

    public void ClearCache()
    {
        MemberRecoveryRateExplanation.Reset();
    }
}