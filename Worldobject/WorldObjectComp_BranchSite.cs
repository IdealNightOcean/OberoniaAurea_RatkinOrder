using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class WorldObjectCompProperties_BranchSite : WorldObjectCompProperties
{
    /// <summary>是否为独立站点</summary>
    /// <remarks>- 若为<see langword="true"/> 当对应 <see cref="Branch"/> 被移除后，对应 <see cref="WorldObjectComp.parent"/> 也将同时被移除</remarks>
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
    public RatkinOrder RatkinOrder => branch?.RatkinOrder;

    public WorldObjectCompProperties_BranchSite Props => (WorldObjectCompProperties_BranchSite)props;

    public bool IsActive => branch.IsValid();

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref branch, "branch");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (!branch.IsValid() && Props.independent)
            {
                parent.SafeDestroy();
            }
        }
    }

    public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
    {
        foreach (Gizmo gizmo in base.GetCaravanGizmos(caravan))
        {
            yield return gizmo;
        }
        if (!IsActive)
        {
            yield break;
        }

        Command_Action command_OpenBranchWindow = new()
        {
            defaultLabel = "OARO_Command_OpenBranchWindow".Translate(),
            defaultDesc = "OARO_Command_OpenBranchWindowDesc".Translate(),
            action = delegate
            {
                Window_Branch branchWindow = new(branch, caravan, map: null);
                Find.WindowStack.Add(branchWindow);
            }
        };
        yield return command_OpenBranchWindow;
    }

    public void SetOrderBranch(Branch newBranch, bool replaceCur)
    {
        try
        {
            if (branch.IsValid())
            {
                if (replaceCur)
                {
                    Branch preBranch = branch;
                    branch = null;
                    preBranch.BranchManager.DestoryBranch(preBranch);
                }
                else
                {
                    Log.Error($"[OARO] {nameof(branch)} has already been set and cannot be assigned again.");
                    return;
                }
            }
        }
        finally
        {
            branch = newBranch;
        }
    }

    public void SetOrderBranch(Branch newBranch) => SetOrderBranch(newBranch, replaceCur: false);

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
                parent.SafeDestroy();
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
                parent.SafeDestroy();
            }
        }
    }
}