namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_FundToAffectRadius : BranchStatPart
{
    public override float PostTransform(Branch branch, float value)
    {
        return value += (branch.RatkinOrder.FundHandler.Funds / 0.08f);
    }
}
