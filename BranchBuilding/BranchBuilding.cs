using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuilding : IExposable
{
    protected BranchBuildingDef def;
    protected Branch branch;

    public BranchBuildingDef Def => def;
    public Branch Branch => branch;
    public RatkinOrder RatkinOrder => branch.RatkinOrder;

    private bool hasUpgraded;
    public bool HasUpgraded
    {
        get => hasUpgraded;
        set => hasUpgraded = value && def.IsUpgradable;
    }

    public string Label => hasUpgraded ? def.advancedProperties.label : def.label;

    protected BranchBuilding() { }

    protected virtual void Initialize(BranchBuildingDef def, Branch branch)
    {
        this.def = def;
        this.branch = branch;
    }

    public virtual void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
        Scribe_References.Look(ref branch, "branch");
        Scribe_Values.Look(ref hasUpgraded, "hasUpgraded", defaultValue: false);
    }

    public static BranchBuilding GenerateBranchBuilding(BranchBuildingDef def, Branch branch)
    {
        BranchBuilding building = (BranchBuilding)Activator.CreateInstance(
            type: def.buildingClass,
            bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance,
            binder: null,
            args: null,
            culture: null);

        building.Initialize(def, branch);
        return building;
    }

    /// <summary>
    /// 仅在添加建筑时触发
    /// </summary>
    public virtual void InitActive() { }

    /// <summary>
    ///  添加建筑和加载存档时触发
    /// </summary>
    public virtual void PostActive() { }

    /// <summary>
    /// 移除建筑时触发
    /// </summary>
    public virtual void PostDeactive() { }

    public virtual void PostUpgraded() { }

    public bool TryGetStatTransformer(BranchStatDef statDef, out BranchStatTransformer transformer)
    {
        transformer = BranchStatTransformer.DefaultTransformer;
        bool hasTransformer = false;

        List<BranchStatModifier> branchStatModifies;
        if (def.branchStatModifies is not null)
        {
            branchStatModifies = def.branchStatModifies;
            for (int i = 0; i < branchStatModifies.Count; i++)
            {
                if (branchStatModifies[i].statDef == statDef)
                {
                    hasTransformer = true;
                    transformer.MergeWith(branchStatModifies[i].Transformer);
                    break;
                }
            }
        }
        if (HasUpgraded && def.advancedProperties?.branchStatModifies is not null)
        {
            branchStatModifies = def.advancedProperties.branchStatModifies;
            for (int i = 0; i < branchStatModifies.Count; i++)
            {
                if (branchStatModifies[i].statDef == statDef)
                {
                    hasTransformer = true;
                    transformer.MergeWith(branchStatModifies[i].Transformer);
                    break;
                }
            }
        }
        return hasTransformer;
    }
}