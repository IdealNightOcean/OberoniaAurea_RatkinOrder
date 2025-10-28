using System;
using Verse;
using static OberoniaAurea.RatkinOrder.BranchDemand;

namespace OberoniaAurea.RatkinOrder;

public class BranchDemandHandler : ITickDay, IExposable
{
    private readonly Branch branch;

    private BranchDemand normalDemand;
    private BranchDemand criticalDemand;

    public BranchDemand NormalDemand => normalDemand;
    public BranchDemand CriticalDemand => criticalDemand;
    public bool HasDemand => normalDemand is not null || criticalDemand is not null;

    internal BranchDemandHandler(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
    }

    public void ExposeData()
    {
        Scribe_Deep.Look(ref normalDemand, "normalDemand");
        Scribe_Deep.Look(ref criticalDemand, "criticalDemand");
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label("NormalDemand");
        if (normalDemand is null)
        {
            listing_Rect.SubLabel("None", 0.8f);
        }
        else
        {
            listing_Rect.SubLabel(normalDemand.ToString(), 0.8f);
            if (listing_Rect.ButtonText("Accept", widthPct: 0.6f))
            {
                if (AcceptedBranchDemandHandler.Instance.CanAcceptDemand(branch, normalDemand))
                {
                    AcceptedBranchDemandHandler.Instance.AcceptDemand(branch, normalDemand);
                }
            }
        }

        listing_Rect.Label("CriticalDemand");
        if (criticalDemand is null)
        {
            listing_Rect.SubLabel("None", 0.8f);
        }
        else
        {
            listing_Rect.SubLabel(criticalDemand.Def.label, 0.8f);
            listing_Rect.SubLabel(criticalDemand.ToString(), 0.8f);
            if (listing_Rect.ButtonText("Accept", widthPct: 0.6f))
            {
                if (AcceptedBranchDemandHandler.Instance.CanAcceptDemand(branch, criticalDemand))
                {
                    AcceptedBranchDemandHandler.Instance.AcceptDemand(branch, criticalDemand);
                }
            }
        }
    }

    internal void PostLoadInit()
    {
        CheckDemand();
    }

    public void TickDay()
    {
        CheckDemand();
        if (normalDemand is null && !branch.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.NormalDemandPeriodic))
        {
            branch.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.NormalDemandPeriodic, cdTicks: 3 * 18000, shouldRemoveWhenExpired: true);
            PeriodicTriggerNewNormalDemand();
        }
    }

    /// <summary>
    /// 检查需求有效性，移除无效需求
    /// </summary>
    private void CheckDemand()
    {
        if (normalDemand?.ShouldRemove ?? false)
        {
            normalDemand = null;
        }
        if (criticalDemand?.ShouldRemove ?? false)
        {
            criticalDemand = null;
        }
    }

    private void PeriodicTriggerNewNormalDemand()
    {
        if (Rand.Chance(0.8f))
        {
            return;
        }
        if (CanAddDemand(isCriticalDemand: false, ignoreCD: false, replaceCur: false))
        {
            DemandType demandType = Rand.Chance(0.1f) ? DemandType.Urgency : DemandType.Normal;
            BranchDemandDef demandDef = BranchDemandUtility.GetRandomBranchDemandOfType(branch, demandType);
            if (demandDef is not null)
            {
                AddNewDemand(demandDef);
            }
        }
    }

    public void AddNewDemand(BranchDemandDef demandDef)
    {
        if (demandDef.IsCriticalDemand)
        {
            criticalDemand = BranchDemand.MakeBranchDemand(demandDef);
            criticalDemand.PostAddToBranch(branch);
            branch.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.CriticalDemandAdd, cdTicks: 30 * 60000, shouldRemoveWhenExpired: true);
        }
        else
        {
            normalDemand = BranchDemand.MakeBranchDemand(demandDef);
            normalDemand.PostAddToBranch(branch);
            branch.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.NormalDemandAdd, cdTicks: 20 * 60000, shouldRemoveWhenExpired: true);
        }

        if (branch.IsBranchOfType(Branch.BranchType.Friendly))
        {
            BranchDemandUtility.FriendyBranchDemandInform(branch, demandDef);
        }
    }

    public bool CanAddDemand(bool isCriticalDemand, bool ignoreCD = false, bool replaceCur = false)
    {
        if (isCriticalDemand)
        {
            if (criticalDemand is not null && !criticalDemand.ShouldRemove)
            {
                if (replaceCur)
                {
                    return !criticalDemand.IsOngoing
                        && (ignoreCD || !branch.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.CriticalDemandAdd));
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return ignoreCD || !branch.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.CriticalDemandAdd);
            }
        }
        else
        {
            if (normalDemand is not null && !normalDemand.ShouldRemove)
            {
                if (replaceCur)
                {
                    return !normalDemand.IsOngoing
                        && (ignoreCD || branch.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.NormalDemandAdd));
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return ignoreCD || branch.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.NormalDemandAdd);
            }
        }
    }

    public void Notify_DemandQuestClean(bool isCritical)
    {
        if (isCritical)
        {
            criticalDemand = null;
        }
        else
        {
            normalDemand = null;
        }
    }
}