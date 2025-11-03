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

    /// <summary>
    /// 仅在建筑升级时触发
    /// </summary>
    public virtual void InitUpgraded() { }

    /// <summary>
    ///  建筑升级时和已升级建筑加载存档时触发
    /// </summary>
    public virtual void PostUpgraded() { }

    public bool TryGetStatTransformer(BranchStatDef statDef, out BranchStatTransformer transformer)
    {
        transformer = BranchStatTransformer.DefaultTransformer;
        bool hasTransformer = false;

        List<BranchStatModifier> branchStatModifies;
        if (def.branchStatOffsets is not null)
        {
            branchStatModifies = def.branchStatOffsets;
            for (int i = 0; i < branchStatModifies.Count; i++)
            {
                if (branchStatModifies[i].statDef == statDef)
                {
                    hasTransformer = true;
                    transformer.MergeOffset(branchStatModifies[i].value);
                    break;
                }
            }
        }
        if (def.branchStatFactors is not null)
        {
            branchStatModifies = def.branchStatFactors;
            for (int i = 0; i < branchStatModifies.Count; i++)
            {
                if (branchStatModifies[i].statDef == statDef)
                {
                    hasTransformer = true;
                    transformer.MergeFactor(branchStatModifies[i].value);
                    break;
                }
            }
        }

        if (HasUpgraded && def.advancedProperties is not null)
        {
            if (def.advancedProperties.branchStatOffsets is not null)
            {
                branchStatModifies = def.advancedProperties.branchStatOffsets;
                for (int i = 0; i < branchStatModifies.Count; i++)
                {
                    if (branchStatModifies[i].statDef == statDef)
                    {
                        hasTransformer = true;
                        transformer.MergeOffset(branchStatModifies[i].value);
                        break;
                    }
                }
            }
            if (def.advancedProperties.branchStatFactors is not null)
            {
                branchStatModifies = def.advancedProperties.branchStatFactors;
                for (int i = 0; i < branchStatModifies.Count; i++)
                {
                    if (branchStatModifies[i].statDef == statDef)
                    {
                        hasTransformer = true;
                        transformer.MergeFactor(branchStatModifies[i].value);
                        break;
                    }
                }
            }
        }
        return hasTransformer;
    }
}