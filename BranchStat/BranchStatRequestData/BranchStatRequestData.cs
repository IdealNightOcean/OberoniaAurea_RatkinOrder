namespace OberoniaAurea.RatkinOrder;

public class BranchStatRequestData : StatRequestData<BranchStatDef, Branch>
{
    public BranchStatRequestData() { }
    public BranchStatRequestData(Branch branch) : base(branch)
    { }
    public BranchStatRequestData(Branch branch, BranchStatDef statDef) : base(branch, statDef)
    { }
}