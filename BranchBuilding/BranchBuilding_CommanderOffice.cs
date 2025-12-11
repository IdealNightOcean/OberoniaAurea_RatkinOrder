namespace OberoniaAurea.RatkinOrder;

public class BranchBuilding_CommanderOffice : BranchBuilding
{
    public override void InitActive()
    {
        base.InitActive();
        branch.CommanderVisitable = true;
    }
}
