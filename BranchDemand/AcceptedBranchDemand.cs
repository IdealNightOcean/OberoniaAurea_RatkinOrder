using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AcceptedBranchDemand : IExposable
{
    public Branch Branch;
    public bool IsCritical;

    public BranchDemand Demand => IsCritical ? Branch.DemandHandler.CriticalDemand : Branch.DemandHandler.NormalDemand;

    private AcceptedBranchDemand() { }
    public AcceptedBranchDemand(Branch branch, BranchDemand demand)
    {
        Branch = branch;
        IsCritical = demand.Def.IsCriticalDemand;
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref Branch, "Branch");
        Scribe_Values.Look(ref IsCritical, "IsCritical", defaultValue: false);
    }
}