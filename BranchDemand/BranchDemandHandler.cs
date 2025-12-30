using RimWorld;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchDemandHandler : ITickDay, IExposable
{
    private readonly Branch branch;

    private BranchDemand normalDemand;
    private BranchDemand_Critical criticalDemand;

    public BranchDemand NormalDemand => normalDemand;
    public BranchDemand_Critical CriticalDemand => criticalDemand;
    public bool HasDemand => normalDemand is not null || criticalDemand is not null;

    internal BranchDemandHandler(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
    }

    public void ExposeData()
    {
        Scribe_Deep.Look(ref normalDemand, nameof(normalDemand));
        Scribe_Deep.Look(ref criticalDemand, nameof(criticalDemand));
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label("日常需求: ");
        if (normalDemand is null)
        {
            listing_Rect.SubLabel("无", 0.8f);
        }
        else
        {
            listing_Rect.SubLabel(normalDemand.Def.label, 0.8f);
            listing_Rect.SubLabel(normalDemand.ToString(), 0.8f);
            if (listing_Rect.ButtonText("Accept".Translate(), widthPct: 0.6f))
            {
                if (BranchDemandUtility.CanAcceptDemand(branch, isCritical: false, resultOnly: true))
                {
                    TryAcceptDemand(isCritical: false);
                }
            }
        }

        listing_Rect.Label("关键需求: ");
        if (criticalDemand is null)
        {
            listing_Rect.SubLabel("None".Translate(), 0.8f);
        }
        else
        {
            listing_Rect.SubLabel(criticalDemand.Def.label, 0.8f);
            listing_Rect.SubLabel(criticalDemand.ToString(), 0.8f);
            if (listing_Rect.ButtonText("Accept".Translate(), widthPct: 0.6f))
            {
                if (BranchDemandUtility.CanAcceptDemand(branch, isCritical: true, resultOnly: true))
                {
                    TryAcceptDemand(isCritical: true);
                }
            }
        }
    }

    internal void PostLoadInit()
    {
        CheckDemand();
    }

    public BranchDemand GetDemand(bool isCritical) => isCritical ? criticalDemand : normalDemand;
    public void RemoveDemand(bool isCritical)
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

    public void TickDay()
    {
        CheckDemand();
        if (normalDemand is null && !branch.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.NormalDemandPeriodic))
        {
            branch.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.NormalDemandPeriodic, cdTicks: 3 * 60000, removeWhenExpired: true);
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
            BranchDemand.DemandType demandType = Rand.Chance(0.1f) ? BranchDemand.DemandType.Urgency : BranchDemand.DemandType.Normal;
            BranchDemandDef demandDef = BranchDemandUtility.GetRandomBranchDemandOfType(branch, demandType);
            if (demandDef is not null)
            {
                AddNewDemand(demandDef);
            }
        }
    }

    public void AddNewDemand(BranchDemandDef demandDef)
    {
        if (demandDef.IsCritical)
        {
            try
            {
                criticalDemand = (BranchDemand_Critical)BranchDemand.MakeBranchDemand(demandDef);
                criticalDemand.PostInit(branch);
                branch.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.CriticalDemandAdd, cdTicks: 30 * 60000, removeWhenExpired: true);
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: "generate or initialize critical demand",
                    typeName: nameof(BranchDemandHandler),
                    methodName: nameof(AddNewDemand),
                    needStackTrace: true);
                criticalDemand = null;
            }
        }
        else
        {
            try
            {
                normalDemand = BranchDemand.MakeBranchDemand(demandDef);
                normalDemand.PostInit(branch);

                branch.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.NormalDemandAdd, cdTicks: 20 * 60000, removeWhenExpired: true);
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: "generate or initialize normal demand",
                    typeName: nameof(BranchDemandHandler),
                    methodName: nameof(AddNewDemand),
                    needStackTrace: true);
                normalDemand = null;
            }
        }

        if (branch.IsBranchOfType(Branch.BranchType.Friendly))
        {
            bool showMessage = demandDef.IsCritical ? RatkinOrderSettings.CriticalDemandShowMess : RatkinOrderSettings.NoramlDemandShowMess;
            if (showMessage)
            {
                Messages.Message(
                    text: "OARO_Message_DemandFriendlyInform".Translate(branch.Name.Named(KeyLibrary_FormatArgName.BranchName), demandDef.Named(KeyLibrary_FormatArgName.DEMAND)),
                    def: MessageTypeDefOf.PositiveEvent);
            }
            if (Rand.Bool && !branch.RatkinOrder.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.DemandFriendlyInform))
            {
                branch.RatkinOrder.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.DemandFriendlyInform, cdTicks: 12 * 60000, removeWhenExpired: true);

                OrderLetterUtility.MakeOrderLetter(label: "OARO_LetterLabel_DemandFriendlyInform".Translate(branch.Name.Named(KeyLibrary_FormatArgName.BranchName)),
                                                   text: "OARO_LetterLabel_DemandFriendlyInform".Translate(
                                                       branch.NameColored.Named(KeyLibrary_FormatArgName.BranchName),
                                                       demandDef.Named(KeyLibrary_FormatArgName.DEMAND)),
                                                   def: OrderLetterDefOf.OARO_OfficialLetter,
                                                   relatedOrder: branch.RatkinOrder,
                                                   relatedBranch: branch,
                                                   sender: branch.NameColored,
                                                   relatedLetterType: OrderLetter.RelatedLetterType.Positive);
            }
        }
    }

    public bool TryAcceptDemand(bool isCritical)
    {
        BranchDemand demand = isCritical ? criticalDemand : normalDemand;
        if (demand is null || demand.IsOngoing)
        {
            return false;
        }
        demand.OnAccepted(branch);
        if (!demand.IsOngoing)
        {
            RemoveDemand(isCritical);
            return false;
        }
        AcceptedBranchDemandHandler.Instance.OnAcceptDemand(branch, isCritical);
        return true;
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
                        && (ignoreCD || !branch.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.NormalDemandAdd));
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return ignoreCD || !branch.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.NormalDemandAdd);
            }
        }
    }
}