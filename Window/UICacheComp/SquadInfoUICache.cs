using RimWorld;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.Branch;

namespace OberoniaAurea.RatkinOrder;

public class SquadInfoUICache : BranchSummaryUICache
{
    public readonly string FriendlyExpireDateStr = string.Empty;
    public readonly float FriendlyProcess = 0f;

    public readonly int CommanderCeiling = -1;
    public readonly int CrewCeiling = -1;
    public readonly float MemberRecoveryRate = -1f;
    public readonly int BombardSupportCeiling = -1;

    public readonly AcceptanceReport CanUnlockSupportAuthority = false;
    public readonly AcceptanceReport CanRequestCombatReadiness = false;
    public readonly AcceptanceReport SupportFeasibility;

    public readonly AcceptanceReport BombardFeasibility = false;

    public SquadInfoUICache() : base() { }

    public SquadInfoUICache(Branch branch, Map map) : base(branch, map)
    {
        if (branch.IsBranchOfType(BranchType.Friendly))
        {
            FriendlyProcess = Mathf.Clamp01(branch.FriendlyDaysLeft / (float)BranchUtility.GetDefaultFriendlyDurationDays(branch));
            FriendlyExpireDateStr = GenDate.SeasonDateStringAt(GenTicks.TicksAbs + branch.FriendlyDaysLeft, Find.WorldGrid.LongLatOf(map.Tile));
        }

        CanUnlockSupportAuthority = BranchUtility.CanUnlockSupportAuthority(branch, map, resultOnly: false);
        CanRequestCombatReadiness = branch.TaskHandler.CanSwitchToTask(BranchTaskDefOf.OARO_CombatReadiness, resultOnly: false);
        BombardFeasibility = BranchSupportUtility.CanBombard(branch, map, resultOnly: false);
        SupportFeasibility = BranchSupportUtility.CanSupport(branch, BranchSupportUtility.SupportLevel.Quarter, map, resultOnly: false);

        CommanderCeiling = (int)branch.Squad.CommanderCeiling;

        MemberRecoveryRate = branch.GetStatValue(BranchStatDefOf.OARO_SquadMemberRecoveryRate);
        BombardSupportCeiling = (int)branch.GetStatValue(BranchStatDefOf.OARO_BombardSupportCeiling);
    }
}