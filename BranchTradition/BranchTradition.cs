using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchTradition : IExposable
{

    protected BranchTraditionDef def;
    public BranchTraditionDef Def => def;

    protected int level;
    public int Level => level;

    private BranchTraditionStage stage;
    public BranchTraditionStage Stage => stage ??= def.GetLevelStage(level);

    protected BranchTradition() { }

    public static BranchTradition GenerateTradition(BranchTraditionDef def)
    {
        BranchTradition tradition = (BranchTradition)Activator.CreateInstance(def.traditionClass);
        tradition.def = def;
        return tradition;
    }

    public virtual void ExposeData()
    {
        Scribe_Defs.Look(ref def, nameof(def));
        Scribe_Values.Look(ref level, nameof(level), 1);
    }

    public virtual bool CanUpgrade(Branch branch)
    {
        if (level >= def.MaxLevel)
        {
            return false;
        }
        if (def.medalDef is not null && branch.MedalHandler.GetMedalCount(def.medalDef) < def.upgradeMedalCost)
        {
            return false;
        }
        return true;
    }

    public virtual void Upgrade(Branch branch)
    {
        if (CanUpgrade(branch))
        {
            level++;
            PostUpgrade(branch);
        }
    }

    public virtual void PostEstablish(Branch branch) { }

    protected virtual void PostUpgrade(Branch branch) { }

    public virtual void ApplyEffects(Branch branch)
    {
        ApplyKnightEffects(branch);
        ApplySquadPotencyBonus(branch);
        ApplyInfluenceEffects(branch);
    }

    protected virtual void ApplyKnightEffects(Branch branch) { }

    protected virtual void ApplySquadPotencyBonus(Branch branch) { }

    protected virtual void ApplyInfluenceEffects(Branch branch) { }

    public virtual void RemoveEffects(Branch branch)
    {
        RemoveKnightEffects(branch);
        RemoveInfluenceEffects(branch);
    }

    protected virtual void RemoveKnightEffects(Branch branch) { }

    protected virtual void RemoveInfluenceEffects(Branch branch) { }

    public virtual int GetEstablishMedalCost(Branch branch)
    {
        if (def.medalDef is null)
            return 0;

        if (def.medalDef == branch.HonorDef?.medalDef)
            return 5;

        return 10;
    }
}
