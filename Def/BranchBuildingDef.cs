using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingDef : BranchConstructionDef
{
    private static readonly Type defaultType = typeof(BranchBuilding);
    private static readonly Type defaultConstructCheckerClass = typeof(BranchBuildingConstructChecker);
    private static readonly BranchBuildingConstructChecker defaultConstructChecker = new();

    public Type buildingClass = defaultType;
    public Type constructCheckerClass = defaultConstructCheckerClass;

    private BranchBuildingConstructChecker constructChecker;
    public BranchBuildingConstructChecker ConstructChecker => constructChecker ??= (constructCheckerClass == defaultConstructCheckerClass) ? defaultConstructChecker : (BranchBuildingConstructChecker)Activator.CreateInstance(constructCheckerClass);

    public int silverCost; //白银花费
    public float constructionDays; //建造所需天数
    public int suggestedMinPopulation;

    public bool IsUpgradable => advancedProperties is not null;
    public BuildingAdvancedProperties advancedProperties;

    public bool isSpecial;

    /// <summary>
    /// 标记荣誉象征，建有该建筑的分部为荣誉分部；
    /// 只在isSpecial为true时生效
    /// </summary>
    public bool IsHonorSymbol => honorDef is not null;
    public BranchHonorDef honorDef;

    public List<string> effectFlags; //效果标志列表
    public List<BranchStatModifier> branchStatOffsets; //属性修正列表（Offset）
    public List<BranchStatModifier> branchStatFactors; //属性修正列表（Factor）
    [MustTranslate]
    public List<string> customEffectDescriptions;

    /// <summary>
    /// comp列表，每个运行时类型只能有一个
    /// 重复会导致报错 + 只有第一个生效
    /// </summary>
    public List<BranchBuildingCompProperties> comps;

    public T GetCompProperties<T>() where T : BranchBuildingCompProperties
    {
        if (comps is null)
        {
            return null;
        }

        for (int i = 0; i < comps.Count; i++)
        {
            if (comps[i] is T compT)
            {
                return compT;
            }
        }

        return null;
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }

        if (!isSpecial && IsHonorSymbol)
        {
            yield return "\"isHonorSymbol\" only works when \"isSpecial\" is true.";
        }

        if (buildingClass is null)
        {
            buildingClass = defaultType;
            yield return "has null buildingClass. Set to default.";
        }
        if (constructCheckerClass is null)
        {
            constructCheckerClass = defaultConstructCheckerClass;
            yield return "has null constructCheckerClass. Set to default.";
        }
        if (comps is not null && comps.Count > 0)
        {
            if (!typeof(BranchBuildingWithComps).IsAssignableFrom(buildingClass))
            {
                yield return "has components but it's buildingClass is not a BranchBuildingWithComps";
            }
            for (int i = 0; i < comps.Count; i++)
            {
                foreach (string compError in comps[i].ConfigErrors(this))
                {
                    yield return compError;
                }
            }
        }
    }

    public IEnumerable<string> GetBaseEffectDescriptions()
    {
        if (branchStatOffsets is not null)
        {
            foreach (BranchStatModifier modifier in branchStatOffsets)
            {
                yield return modifier.ToStringOffset();
            }
        }

        if (branchStatFactors is not null)
        {
            foreach (BranchStatModifier modifier in branchStatFactors)
            {
                yield return modifier.ToStringFactor();
            }
        }

        if (customEffectDescriptions is not null)
        {
            foreach (string desc in customEffectDescriptions)
            {
                yield return desc;
            }
        }
    }

    public IEnumerable<string> GetAdvancedEffectDescriptions()
    {
        if (!IsUpgradable)
        {
            yield break;
        }
        if (advancedProperties.branchStatOffsets is not null)
        {
            foreach (BranchStatModifier modifier in advancedProperties.branchStatOffsets)
            {
                yield return modifier.ToStringOffset();
            }
        }

        if (advancedProperties.branchStatFactors is not null)
        {
            foreach (BranchStatModifier modifier in advancedProperties.branchStatFactors)
            {
                yield return modifier.ToStringFactor();
            }
        }

        if (advancedProperties.customEffectDescriptions is not null)
        {
            foreach (string desc in advancedProperties.customEffectDescriptions)
            {
                yield return desc;
            }
        }
    }
}