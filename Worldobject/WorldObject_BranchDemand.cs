using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class WorldObject_BranchDemand : WorldObject_InteractWithFixedCaravan_Nameable, ISingleBranchRelated
{
    protected Branch branch;
    public Branch Branch => branch;
    protected virtual bool DestroyWhenBranchRemoved => true;

    [Unsaved] protected QuestPart_EffectTags effectTags;
    public QuestPart_EffectTags EffectTags
    {
        get
        {
            if (effectTags is null)
            {
                QuestPart_EffectTags.TryGetEffectTags(quest, addPartIfMiss: false, out effectTags);
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
                QuestPart_CliquesManager.TryGetCliquesManager(quest, addPartIfMiss: false, out cliquesManager);
            }
            return cliquesManager;
        }
    }

    public float TotalPotency => CliquesManager?.TotalPotency ?? 0f;

    public void InitOrderBranch(Branch branch)
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

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref branch, "branch");
    }
}