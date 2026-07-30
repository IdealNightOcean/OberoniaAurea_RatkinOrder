using OberoniaAurea.RatkinOrder.Utility;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 执勤协助交互点 - 支持固定远行队交互
/// </summary>
public class WorldObject_JurisdictionDutySite : WorldObject_InteractWithFixedCaravan_Nameable
{
    private BranchTask_JurisdictionDuty relatedDuty;
    public BranchTask_JurisdictionDuty RelatedDuty => relatedDuty;

    public override int TicksNeeded => 5000;

    public void SetDutyWorker(BranchTask_JurisdictionDuty duty)
    {
        relatedDuty = duty;
        name = "OARO_DutySiteName".Translate(duty.Branch?.Name);
    }

    public override bool StartWork(Caravan caravan)
    {
        if (relatedDuty is null)
        {
            this.SafeDestroy();
            return false;
        }
        if (base.StartWork(caravan))
        {
            relatedDuty.Notify_CaravanStartedWork();
            return true;
        }
        return false;
    }

    protected override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (relatedDuty is null)
        {
            this.SafeDestroy();
        }
    }

    protected override void WorkTickInterval(int delta)
    {
        if ((ticksRemaining -= delta) <= 0)
        {
            ticksRemaining = TicksNeeded;
            if (relatedDuty is null || !relatedDuty.IsOngoing)
            {
                EndWork();
            }
        }
    }

    protected override void InterruptWork() { }

    protected override void FinishWork() { }

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

    public override void ExposeData()
    {
        base.ExposeData();
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (relatedDuty is null)
            {
                this.SafeDestroy();
            }
        }
    }
}
