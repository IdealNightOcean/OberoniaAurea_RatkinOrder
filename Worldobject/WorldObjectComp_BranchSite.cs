using RimWorld;
using RimWorld.Planet;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class WorldObjectCompProperties_BranchSite : WorldObjectCompProperties
{
    public bool independent;
    public WorldObjectCompProperties_BranchSite()
    {
        compClass = typeof(WorldObjectComp_BranchSite);
    }
}

public class WorldObjectComp_BranchSite : WorldObjectComp, ISingleBranchRelated
{
    private Branch branch;
    public Branch Branch => branch;

    public WorldObjectCompProperties_BranchSite Props => (WorldObjectCompProperties_BranchSite)props;

    public bool IsActive => branch is not null;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref branch, "branch");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (branch is null && Props.independent)
            {
                parent.Destroy();
            }
        }
    }

    public void InitOrderBranch(Branch newBranch)
    {
        if (branch is not null)
        {
            throw new InvalidOperationException($"{nameof(branch)} has already been set and cannot be assigned again.");
        }
        branch = newBranch;
    }

    public override void PostDestroy()
    {
        if (branch is null)
        {
            return;
        }

        Branch preBranch = branch;
        branch = null;
        preBranch.BranchManager.DestoryBranch(preBranch);
    }

    public void Notify_BranchDestroyed(Branch branch)
    {
        if (this.branch == branch)
        {
            this.branch = null;
            if (Props.independent)
            {
                parent.Destroy();
            }
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (branch?.RatkinOrder == ratkinOrder)
        {
            branch = null;
            if (Props.independent)
            {
                parent.Destroy();
            }
        }
    }
}
