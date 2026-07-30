using OberoniaAurea.RatkinOrder.Utility;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class WorldObject_CriticalBranchDemand : WorldObject_InteractWithFixedCaravan_Nameable, ISingleBranchRelated
{
    protected Branch branch;
    public Branch Branch => branch;
    protected virtual bool DestroyWhenBranchRemoved => true;

    protected virtual int PeriodicCheckInterval => 60000;

    protected int nextCheckTick;

    [Unsaved] protected QuestPart_EffectTags effectTags;
    public QuestPart_EffectTags EffectTags
    {
        get
        {
            if (effectTags is null)
            {
                quest.TryGetEffectTagsPart(addPartIfMiss: false, out effectTags);
            }
            return effectTags;
        }
    }


    [Unsaved] protected QuestPart_CliquesManager cliquesManager;
    public QuestPart_CliquesManager CliquesManager
    {
        get
        {
            if (cliquesManager is null)
            {
                quest.TryGetCliquesManager(addPartIfMiss: false, out cliquesManager);
            }
            return cliquesManager;
        }
    }

    public float TotalPotency => CliquesManager?.TotalPotency.Value ?? 0f;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref branch, "branch");
        Scribe_Values.Look(ref nextCheckTick, "nextCheckTick");
    }

    public override void PostAdd()
    {
        base.PostAdd();
        nextCheckTick = Find.TickManager.TicksGame + PeriodicCheckInterval;
    }

    public void SetOrderBranch(Branch branch)
    {
        this.branch = branch;
    }

    public bool HasQuestEffectTag(string tagKey) => EffectTags?.HasTag(tagKey) ?? false;

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (branch?.RatkinOrder == ratkinOrder)
        {
            branch = null;
            if (DestroyWhenBranchRemoved)
            {
                this.SafeDestroy();
            }
        }
    }

    protected override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (!Destroyed && Find.TickManager.TicksGame > nextCheckTick)
        {
            nextCheckTick = Find.TickManager.TicksGame + PeriodicCheckInterval;
            PeriodicCheck();
        }
    }

    protected abstract void PeriodicCheck();

    public void Notify_BranchDestroyed(Branch branch)
    {
        if (this.branch == branch)
        {
            this.branch = null;
            if (DestroyWhenBranchRemoved)
            {
                this.SafeDestroy();
            }
        }
    }
}