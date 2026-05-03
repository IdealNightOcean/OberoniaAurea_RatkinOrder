using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 执勤协助交互点 - 支持固定远行队交互
/// </summary>
public class WorldObject_JurisdictionDutySite : WorldObject_InteractWithFixedCaravan_Nameable, ISingleBranchRelated
{
    private Branch branch;
    public Branch Branch => branch;

    private BranchTask_JurisdictionDuty relatedDuty;
    public BranchTask_JurisdictionDuty RelatedDuty => relatedDuty;

    public override int TicksNeeded => 5000;

    public void InitDutySite(Branch branch, BranchTask_JurisdictionDuty duty)
    {
        this.branch = branch;
        relatedDuty = duty;
        name = "OARO_DutySiteName".Translate(branch?.Name);
    }

    public void SetOrderBranch(Branch branch) => this.branch = branch;

    public override void Notify_CaravanArrived(Caravan caravan)
    {
        if (!branch.IsValid() || relatedDuty is null)
        {
            return;
        }
        if (isWorking)
        {
            Messages.Message("OAFrame_Message_AlreadyHasFixedCaravan".Translate(), MessageTypeDefOf.RejectInput, historical: false);
        }
        else if (caravan.IsExactTypeCaravan())
        {
            StartWork(caravan);
        }
    }

    public override bool StartWork(Caravan caravan)
    {
        if (base.StartWork(caravan))
        {
            relatedDuty?.Notify_CaravanStartedWork();
            return true;
        }
        return false;
    }

    protected override void InterruptWork()
    {
        relatedDuty?.Notify_CaravanInterruptedWork(associatedFixedCaravan);
    }

    protected override void FinishWork()
    {
        relatedDuty?.Notify_CaravanFinishedWorkCycle(associatedFixedCaravan);
        if (relatedDuty is not null && relatedDuty.IsOngoing)
        {
            ticksRemaining = TicksNeeded;
        }
    }

    public override string GetInspectString()
    {
        string baseStr = base.GetInspectString();
        if (relatedDuty is not null)
        {
            JurisdictionDutyData data = relatedDuty.DutyData;
            if (data is not null)
            {
                baseStr += $"\n{"OARO_DutyProgress".Translate()}: {data.CurProgress}/{data.ProgressCeiling}";
            }
        }
        return baseStr;
    }

    public void Notify_BranchDestroyed(Branch branch)
    {
        if (this.branch == branch)
        {
            this.branch = null;
            this.SafeDestroy();
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (branch?.RatkinOrder == ratkinOrder)
        {
            branch = null;
            this.SafeDestroy();
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref branch, nameof(branch));
        // Scribe_References.Look(ref relatedDuty, nameof(relatedDuty));
    }
}
