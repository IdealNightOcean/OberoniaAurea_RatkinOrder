using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchRelatedRecord : IExposable
{
    private Branch branch;
    public Branch Branch => branch;
    protected BranchRelatedRecord() { }
    public BranchRelatedRecord(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
    }

    public virtual void ExposeData()
    {
        Scribe_References.Look(ref branch, "branch");
    }
}