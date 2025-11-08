using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Utility;
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

    public readonly Texture2D MedalBackground;

    public readonly Texture2D HonorExpandIcon;
    public readonly Texture2D HonorStrip;
    public readonly Texture2D HonorBackground;
    public readonly Texture2D HonorDecoration;

    public readonly AcceptanceReport CanUnlockSupportAuthority = false;
    public readonly AcceptanceReport CanRequestCombatReadiness = false;
    public readonly List<AcceptanceReport> SupportFeasibilities;
    public AcceptanceReport SupportFeasibility => SupportFeasibilities?.FirstOrFallback(fallback: false) ?? false;
    public readonly AcceptanceReport BombardFeasibility = false;

    public SquadInfoUICache() : base() { }

    public SquadInfoUICache(Branch branch, Map map) : base(branch, map)
    {
        if (branch.IsBranchOfType(BranchType.Friendly))
        {
            FriendlyProcess = Mathf.Clamp01(branch.FriendlyDaysLeft / (float)BranchUtility.GetDefaultFriendlyDurationDays(branch));
            FriendlyExpireDateStr = GenDate.SeasonDateStringAt(GenTicks.TicksAbs + branch.FriendlyDaysLeft, Find.WorldGrid.LongLatOf(map.Tile));
        }

        HonorExpandIcon = branch.HonorDef?.ExpandingIconTexture;
        BranchMedalRecord.BranchMedalType primaryMedal = branch.MedalHandler.PrimaryMedal;
        if (primaryMedal != BranchMedalRecord.BranchMedalType.None)
        {
            MedalBackground = new CachedTexture($"UI/Medal/OARO_MedalBackground_{primaryMedal}").Texture;
            if (branch.IsBranchOfType(BranchType.Honor))
            {
                HonorStrip = new CachedTexture($"UI/BranchCommon/OARO_HonorStrip_{primaryMedal}").Texture;
                HonorBackground = new CachedTexture($"UI/BranchCommon/OARO_HonorBackground_{primaryMedal}").Texture;
                HonorDecoration = new CachedTexture($"UI/BranchCommon/OARO_HonorDecoration_{primaryMedal}").Texture;
            }
        }

        CanUnlockSupportAuthority = BranchUtility.CanUnlockSupportAuthority(branch, map, resultOnly: false);
        CanRequestCombatReadiness = branch.TaskHandler.CanSwitchToTask(BranchTaskDefOf.OARO_CombatReadiness, resultOnly: false);
        BombardFeasibility = BranchSupportUtility.CanBombard(branch, map, resultOnly: false);

        List<BranchSupportUtility.SupportLevel> supportLevels = EnumUtility.GetValues<BranchSupportUtility.SupportLevel>().ToList();
        SupportFeasibilities = new(supportLevels.Count);
        for (int i = 0; i < supportLevels.Count; i++)
        {
            SupportFeasibilities.Add(BranchSupportUtility.CanSupport(branch, supportLevels[i], map, resultOnly: false));
        }

        CommanderCeiling = (int)branch.Squad.CommanderCeiling;
        CrewCeiling = (int)branch.Squad.MemberCeiling + CommanderCeiling;

        MemberRecoveryRate = branch.GetStatValue(BranchStatDefOf.OARO_SquadMemberRecoveryRate);
        BombardSupportCeiling = (int)branch.GetStatValue(BranchStatDefOf.OARO_BombardSupportCeiling);
    }
}