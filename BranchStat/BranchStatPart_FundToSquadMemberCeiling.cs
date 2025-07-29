namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_FundToSquadMemberCeiling : BranchStatPart
{
    public override float PostTransform(Branch branch, float value)
    {
        return value += branch.RatkinOrder.FundHandler.Funds;
    }
}