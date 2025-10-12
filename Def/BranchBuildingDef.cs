using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingDef : Def
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

    public bool isSpecial;

    /// <summary>
    /// 标记荣誉象征，建有该建筑的分部为荣誉分部；
    /// 只在isSpecial为true时生效
    /// </summary>
    public bool isHonorSymbol;
    public HonorBranchProperties honorProperties;

    public List<string> effectFlags; //效果标志列表
    public List<BranchStatModifier> branchStatModifies; //属性修正列表

    /// <summary>
    /// comp列表，每个运行时类型只能有一个
    /// 重复会导致报错 + 只有第一个生效
    /// </summary>
    public List<BranchBuildingCompProperties> comps;

    public bool GetBranchStatModifierOfDef(BranchStatDef statDef, out BranchStatModifier statModifier)
    {
        statModifier = null;
        if (branchStatModifies is null)
        {
            return false;
        }

        for (int i = 0; i < branchStatModifies.Count; i++)
        {
            if (branchStatModifies[i].statDef == statDef)
            {
                statModifier = branchStatModifies[i];
                return true;
            }
        }

        return false;
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }

        if (!isSpecial && isHonorSymbol)
        {
            yield return "\"isHonorSymbol\" only works when \"isSpecial\" is true.";
        }

        if (isHonorSymbol && honorProperties is null)
        {
            yield return "is an HonorSymbol building but does not have honorProperties.";
        }

        if (!isHonorSymbol && honorProperties is not null)
        {
            yield return "is not an HonorSymbol building but has honorProperties.";
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
}