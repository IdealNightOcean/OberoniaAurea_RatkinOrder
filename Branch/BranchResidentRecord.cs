using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchResidentRecord : IExposable
{
    public Pawn Resident;

    private int totalDeployDays;
    public int TotalDeployDays => totalDeployDays;

    public int DeployDaysLeft;

    public ResidencyWorker ResidencyWorker;

    public BranchResidentRecord() { }
    public BranchResidentRecord(Pawn resident, int totalDeployDays, ResidencyWorker worker)
    {
        Resident = resident;
        this.totalDeployDays = totalDeployDays;
        DeployDaysLeft = totalDeployDays;
        ResidencyWorker = worker;
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref Resident, "Resident");
        Scribe_Values.Look(ref totalDeployDays, "totalDeployDays", 0);
        Scribe_Values.Look(ref DeployDaysLeft, "DeployDaysLeft", 0);
        Scribe_Deep.Look(ref ResidencyWorker, "ResidencyWorker");
    }
}