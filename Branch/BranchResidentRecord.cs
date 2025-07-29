using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchResidentRecord : IExposable
{
    public Pawn resident;

    private int totalDeployDays;
    public int TotalDeployDays => totalDeployDays;

    public int deployDaysLeft;

    public ResidencyWorker residencyWorker;

    public BranchResidentRecord() { }
    public BranchResidentRecord(Pawn resident, int totalDeployDays, ResidencyWorker worker)
    {
        this.resident = resident;
        this.totalDeployDays = totalDeployDays;
        this.deployDaysLeft = totalDeployDays;
        this.residencyWorker = worker;
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref resident, "resident");
        Scribe_Values.Look(ref totalDeployDays, "totalDeployDays", 0);
        Scribe_Values.Look(ref deployDaysLeft, "deployDaysLeft", 0);
        Scribe_Deep.Look(ref residencyWorker, "residencyWorker");
    }
}