using System;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatRequestData_BranchConstruction<T> : BranchStatRequestData where T : BranchConstructionDef, new()
{
    public T ConstructionDef { get; set; }


    public BranchStatRequestData_BranchConstruction() { }
    public BranchStatRequestData_BranchConstruction(Branch branch, BranchStatDef statDef, T constructionDef) : base(branch, statDef)
    {
        ConstructionDef = constructionDef ?? throw new ArgumentNullException(nameof(constructionDef));
    }
}
