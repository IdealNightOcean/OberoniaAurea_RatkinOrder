using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AcceptedBranchDemand : BranchRelatedRecord
{
    public bool IsCritical;
    public BranchDemand Demand => IsCritical ? Branch.DemandHandler.CriticalDemand : Branch.DemandHandler.NormalDemand;

    private AcceptedBranchDemand() : base() { }
    public AcceptedBranchDemand(Branch branch, bool isCritical) : base(branch)
    {
        IsCritical = isCritical;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref IsCritical, "IsCritical", defaultValue: false);
    }
}