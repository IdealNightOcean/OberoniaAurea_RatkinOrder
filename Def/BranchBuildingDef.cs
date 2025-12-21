using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingDef : BranchConstructionDef
{
    private static readonly Type defaultType = typeof(BranchBuilding);
    private static readonly Type defaultConstructCheckerClass = typeof(BranchBuildingConstructChecker);
    private static readonly BranchBuildingConstructChecker defaultConstructChecker = new();

    /// <summary>
    /// 建筑功能类
    /// </summary>
    public Type buildingClass = defaultType;
    public Type constructCheckerClass = defaultConstructCheckerClass;

    /// <summary>建筑检测类，负责检测<see cref="Branch"/>是否可以建设该建筑</summary>
    /// <remarks>- 建设的二次确认</remarks>
    private BranchBuildingConstructChecker constructChecker;
    public BranchBuildingConstructChecker ConstructChecker => constructChecker ??= (constructCheckerClass == defaultConstructCheckerClass) ? defaultConstructChecker : (BranchBuildingConstructChecker)Activator.CreateInstance(constructCheckerClass);

    /// <summary>
    /// 建设白银花费（基础）
    /// </summary>
    public int silverCost;

    /// <summary> 
    /// 建设所需天数（基础）
    /// </summary>
    public float constructionDays;

    /// <summary>推荐建造人数</summary>
    /// <remarks>分部人口（<see cref="BranchPopulationHandler.Population"/>）低于该值会使建设成本增加</remarks>
    public int suggestedMinPopulation;

    /// <summary>
    /// 是否可升级
    /// </summary>
    public bool IsUpgradable => advancedProperties is not null;

    /// <summary>
    /// 升级参数
    /// </summary>
    public BuildingAdvancedProperties advancedProperties;

    /// <summary>是否为特殊建筑</summary>
    /// <remarks>
    /// <para>- 特殊建筑不占用建筑上限</para>
    /// <para>- 每个分部只能有一个特殊建筑</para>
    /// </remarks>
    public bool isSpecial;

    /// <summary>是否为荣誉象征建筑</summary>
    /// <remarks>
    /// <para>- 建有该建筑的分部为荣誉分部</para>
    /// <para>- 只在<see cref="isSpecial"/> 为 <see langword="true"/> 时生效</para>
    /// </remarks>
    public bool IsHonorSymbol => honorDef is not null;

    /// <summary>
    /// 荣誉对应<see cref="BranchHonorDef"/>
    /// </summary>
    public BranchHonorDef honorDef;

    /// <summary>
    /// 效果标志列表
    /// </summary>
    public List<string> effectFlags; //效果标志列表

    /// <summary>
    /// 属性修正列表（Offset）
    /// </summary>
    public List<BranchStatModifier> branchStatOffsets;

    /// <summary>
    /// 属性修正列表（Factor）
    /// </summary>
    public List<BranchStatModifier> branchStatFactors;

    /// <summary>
    /// 分部界面问候段落 - 建筑部分
    /// </summary>
    [MustTranslate]
    public string greetingParagraph;
    /// <summary>额外自定义的效果描述</summary>
    /// <remarks>- 显示在修正效果之后</remarks>
    [MustTranslate]
    public List<string> customEffectDescriptions;

    /// <summary><see cref="BranchBuildingCompProperties"/>列表，可为<see langword="null"/></summary>
    /// <remarks>- 只在<see cref="buildingClass"/>为<see cref="BranchBuildingWithComps"/>或其子类时生效</remarks>
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

    /// <summary>
    /// 基础等级的修正描述
    /// </summary>
    public IEnumerable<string> GetBaseEffectDescriptions()
    {
        if (customEffectDescriptions is not null)
        {
            foreach (string desc in customEffectDescriptions)
            {
                yield return desc;
            }
        }

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
    }

    /// <summary>繁荣等级的修正描述</summary>
    /// <remarks>- 不包含 <see cref="GetBaseEffectDescriptions"/> 的内容</remarks>
    public IEnumerable<string> GetAdvancedEffectDescriptions()
    {
        if (!IsUpgradable)
        {
            yield break;
        }

        if (advancedProperties.customEffectDescriptions is not null)
        {
            foreach (string desc in advancedProperties.customEffectDescriptions)
            {
                yield return desc;
            }
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
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }

        if (!isSpecial && IsHonorSymbol)
        {
            yield return $"'{nameof(IsHonorSymbol)}' only works when '{nameof(isSpecial)}' is true.";
        }

        if (buildingClass is null)
        {
            buildingClass = defaultType;
            yield return $"has null '{nameof(buildingClass)}'. Set to default.";
        }
        if (constructCheckerClass is null)
        {
            constructCheckerClass = defaultConstructCheckerClass;
            yield return $"has null '{nameof(constructCheckerClass)}'. Set to default.";
        }
        if (comps is not null && comps.Count > 0)
        {
            if (!typeof(BranchBuildingWithComps).IsAssignableFrom(buildingClass))
            {
                yield return $"has {nameof(comps)} defined, but its '{nameof(buildingClass)}' is not '{nameof(BranchBuildingWithComps)}' or its subclass.";
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
}