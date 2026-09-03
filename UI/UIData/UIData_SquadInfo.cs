using NightOcean;
using OberoniaAurea.RatkinOrder.Utility;
using RimWorld;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.Branch;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIData_SquadInfo : UIData_BranchSummary
{
    public float FriendlyProcess { get; protected set; }
    public string FriendlyExpireDateStr { get; protected set; } = string.Empty;

    public float MemberRecoveryRate { get; protected set; } = -1f;

    public LazyMutable<string> MemberRecoveryRateExplanation { get; }

    public UIData_SquadInfo(Branch branch, Map map) : base(branch, map)
    {
        MemberRecoveryRateExplanation = new(refreshFunc: RefreshMemberRecoveryRateExplanation);
    }

    protected override UIDataState RefreshInner()
    {
        UIDataState dataState = base.RefreshInner();
        if (dataState != UIDataState.Ready)
            return dataState;

        if (this.Branch.IsBranchOfType(BranchType.Friendly))
        {
            FriendlyProcess = Mathf.Clamp01(this.Branch.FriendlyDaysLeft / (float)BranchUtility.GetDefaultFriendlyDurationDays(this.Branch));
            FriendlyExpireDateStr = GenDate.SeasonDateStringAt(GenTicks.TicksAbs + this.Branch.FriendlyDaysLeft, Find.WorldGrid.LongLatOf(this.Map.Tile));
        }

        MemberRecoveryRate = this.Branch.GetStatValue(BranchStatDefOf.OARO_SquadMemberRecoveryRate, immediateUpdate: true);
        MemberRecoveryRateExplanation.MarkDirty();

        return UIDataState.Ready;
    }

    private string RefreshMemberRecoveryRateExplanation()
    {
        BranchStatRequestData requestData = new(this.Branch, BranchStatDefOf.OARO_SquadMemberRecoveryRate);

        (string explanation, float? resultNullable) = BranchStatDefOf.OARO_SquadMemberRecoveryRate.GetStatModifyExplanation(requestData);

        MemberRecoveryRate = resultNullable ?? ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor.baseValue;

        return explanation;
    }
}