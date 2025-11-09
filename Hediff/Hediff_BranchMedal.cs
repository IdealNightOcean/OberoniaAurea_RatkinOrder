using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_BranchMedal : HediffWithComps, ISingleBranchRelated
{
    private Branch branch;
    public Branch Branch => branch;
    public override HediffStage CurStage => branch?.MedalHandler.MedalHediffStage;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref branch, "branch");
    }

    public void SetOrderBranch(Branch branch)
    {
        this.branch = branch;
        _ = CurStage;
    }

    public void Notify_BranchDestroyed(Branch branch)
    {
        if (this.branch == branch)
        {
            pawn.health.RemoveHediff(this);
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (branch?.RatkinOrder == ratkinOrder)
        {
            pawn.health.RemoveHediff(this);
        }
    }
}