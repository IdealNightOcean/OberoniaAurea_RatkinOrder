using RimWorld;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AcceptedBranchDemand : IExposable
{
    private Branch branch;
    private bool isCritical;
    private BranchDemand demand;

    public Branch Branch => branch;
    public bool IsCritical => isCritical;
    public BranchDemand Demand => demand ??= (IsCritical ? branch.DemandHandler.CriticalDemand : branch.DemandHandler.NormalDemand);

    public bool IsValid => branch.IsValid() && Demand is not null && !Demand.ShouldRemove;

    private AcceptedBranchDemand() : base() { }
    public AcceptedBranchDemand(Branch branch, bool isCritical)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
        this.isCritical = isCritical;
    }

    public void Notify_DemandQuestPreCleanup(Quest quest)
    {

    }

    public void ExposeData()
    {
        Scribe_References.Look(ref branch, nameof(branch));
        Scribe_Values.Look(ref isCritical, nameof(isCritical), defaultValue: false);
    }
}