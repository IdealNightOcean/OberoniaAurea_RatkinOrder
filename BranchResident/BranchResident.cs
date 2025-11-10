using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class BranchResident : IExposable
{
    public abstract int Priority { get; }

    protected Pawn resident;
    public Pawn Resident => resident;

    protected int totalDeployDays;
    public int TotalDeployDays => totalDeployDays;

    public int DeployDaysLeft;

    public float Progress => totalDeployDays > 0f ? Mathf.Clamp01(DeployDaysLeft / totalDeployDays) : 0f;

    protected BranchResident() { }
    protected BranchResident(Pawn resident, int totalDeployDays)
    {
        this.resident = resident;
        this.totalDeployDays = totalDeployDays;
    }

    public virtual void ExposeData()
    {
        Scribe_References.Look(ref resident, "resident");
        Scribe_Values.Look(ref totalDeployDays, "totalDeployDays", 0);
        Scribe_Values.Look(ref DeployDaysLeft, "DeployDaysLeft", 0);
    }

    public virtual void StartResidency(Branch branch)
    {
        DeployDaysLeft = totalDeployDays;
    }

    public abstract void EndResidency(Branch branch);
}