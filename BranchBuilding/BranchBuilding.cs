using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuilding : IExposable
{
    protected BranchBuildingDef def;
    protected Branch branch;

    public BranchBuildingDef Def => def;
    public Branch Branch => branch;
    public RatkinOrder RatkinOrder => branch.RatkinOrder;

    protected bool hasUpgraded;
    public bool HasUpgraded
    {
        get => hasUpgraded;
        set => hasUpgraded = value && def.IsUpgradable;
    }

    public string Label => (hasUpgraded && def.advancedProperties.label is not null)
        ? def.advancedProperties.label
        : def.label;

    public string Description => (hasUpgraded && def.advancedProperties.description is not null)
        ? def.advancedProperties.description
        : def.description;

    public bool HasGreetingParagraph => !def.greetingParagraph.NullOrEmpty() || (hasUpgraded && !def.advancedProperties.greetingParagraph.NullOrEmpty());
    public string GreetingParagraph
    {
        get
        {
            if (hasUpgraded && !def.advancedProperties.greetingParagraph.NullOrEmpty())
            {
                return def.advancedProperties.greetingParagraph.Formatted(
                    Label.Named("BuildingLabel"),
                    branch.RatkinOrder.Named(OARO_KeyLibrary_FormatArgName.OrderName),
                    branch.NameColored.Named(OARO_KeyLibrary_FormatArgName.BranchName));
            }
            else
            {
                return def.greetingParagraph?.Formatted(
                    Label.Named("BuildingLabel"),
                    branch.RatkinOrder.Named(OARO_KeyLibrary_FormatArgName.OrderName),
                    branch.NameColored.Named(OARO_KeyLibrary_FormatArgName.BranchName));
            }
        }
    }

    protected virtual void Initialize(BranchBuildingDef def, Branch branch)
    {
        this.def = def;
        this.branch = branch;
    }

    public virtual void ExposeData()
    {
        Scribe_Defs.Look(ref def, nameof(def));
        Scribe_References.Look(ref branch, nameof(branch));
        Scribe_Values.Look(ref hasUpgraded, nameof(hasUpgraded), defaultValue: false);
    }

    public static BranchBuilding GenerateBranchBuilding(BranchBuildingDef def, Branch branch)
    {
        BranchBuilding building = (BranchBuilding)Activator.CreateInstance(type: def.buildingClass);
        building.Initialize(def, branch);
        return building;
    }

    /// <summary>
    /// 仅在首次添加建筑时触发
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
    /// 仅在首次建筑升级时触发
    /// </summary>
    public virtual void InitUpgraded() { }

    /// <summary>
    ///  建筑升级时和已升级建筑加载存档时触发
    /// </summary>
    public virtual void PostUpgraded() { }

    public bool TryGetStatTransformer(BranchStatDef statDef, out StatTransformer transformer)
    {
        transformer = new();
        bool hasTransformer = false;

        List<StatModifier<BranchStatDef>> branchStatModifies;
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