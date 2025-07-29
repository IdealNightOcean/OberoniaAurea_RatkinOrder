using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class WorldObjectCompProperties_BranchSite : WorldObjectCompProperties
{
    public WorldObjectCompProperties_BranchSite()
    {
        compClass = typeof(WorldObjectComp_BranchSite);
    }
}

public class WorldObjectComp_BranchSite : WorldObjectComp
{
    private Branch branch;
    public Branch Branch => branch;

    public bool IsActive => branch is not null;

    public bool SetBranch(Branch newBranch)
    {
        if (branch is not null)
        {
            Log.Error($"WorldObjectComp_BranchSite already has a branch assigned: {branch}. Cannot assign a new one.");
            return false;
        }
        branch = newBranch;
        return true;
    }

    public override void PostDestroy()
    {
        if (branch is null)
        {
            return;
        }

        branch.Destroy();
        ClearBranch();
    }

    public void Notify_BranchDestroyed()
    {
        ClearBranch();
    }

    private void ClearBranch()
    {
        if (branch is null)
        {
            return;
        }
        branch = null;
    }
}
