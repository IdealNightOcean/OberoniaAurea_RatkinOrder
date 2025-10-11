using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_Knight : HediffWithComps, IOnRatkinOrderRemoved, IOnBranchDestroyed
{
    private RatkinOrder ratkinOrder;
    private Branch branch;
    private bool isCommander;

    public RatkinOrder RatkinOrder => ratkinOrder;
    public Branch Branch => branch;
    public Squad Squad => branch?.Squad;
    public bool IsCommander => isCommander;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref ratkinOrder, "ratkinOrder");
        Scribe_References.Look(ref branch, "branch");
        Scribe_Values.Look(ref isCommander, "isCommander", defaultValue: false);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (ratkinOrder is null || !pawn.CanBeKnight())
            {
                pawn.health.RemoveHediff(this);
            }
            else
            {
                GameComponent_RatkinOrder.Instance.KnightPawns.Add(pawn);
            }
        }
    }

    public void InitKnightHediff(RatkinOrder ratkinOrder, Branch branch = null, bool isCommander = false)
    {
        this.ratkinOrder = ratkinOrder;

        Log.Message($"Hediff_Knight InitRatkinOrder {pawn.Name}");
        if (ratkinOrder is null || !pawn.CanBeKnight())
        {
            this.ratkinOrder = null;
            pawn.health.RemoveHediff(this);
            return;
        }

        GameComponent_RatkinOrder.Instance.KnightPawns.Add(pawn);

        this.isCommander = isCommander;
        if (isCommander)
        {
            GameComponent_RatkinOrder.Instance.KnightCommanderPawns.Add(pawn);
        }
        if (branch is not null)
        {
            this.branch = branch;
        }
    }

    public void InitKnightHediff(Branch branch, bool isCommander = false) => InitKnightHediff(branch?.RatkinOrder, branch, isCommander);

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (this.ratkinOrder == ratkinOrder)
        {
            pawn.health.RemoveHediff(this);
        }
    }

    public void Notify_BranchDestroyed(Branch branch)
    {
        if (this.branch == branch)
        {
            this.branch = null;
        }
    }

    public override void PostRemoved()
    {
        Log.Message($"Hediff_Knight PostRemoved {pawn.Name}");
        GameComponent_RatkinOrder.Instance.KnightPawns.Remove(pawn);
        if (isCommander)
        {
            GameComponent_RatkinOrder.Instance.KnightCommanderPawns.Remove(pawn);
        }
        base.PostRemoved();
    }
}