using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingDef : Def
{
    private static readonly Type DefaultConstructCheckerClass = typeof(BranchBuildingConstructChecker);
    private static readonly BranchBuildingConstructChecker DefaultConstructChecker = new();

    public Type buildingClass = typeof(BranchBuilding);

    public Type constructCheckerClass = DefaultConstructCheckerClass;
    private BranchBuildingConstructChecker constructChecker;
    public BranchBuildingConstructChecker ConstructChecker => constructChecker ??= (constructCheckerClass == DefaultConstructCheckerClass) ? DefaultConstructChecker : (BranchBuildingConstructChecker)Activator.CreateInstance(constructCheckerClass);

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
            yield return $"{defName}: isHonorSymbol only works when isSpecial is true.";
        }

        if (isHonorSymbol && honorProperties is null)
        {
            yield return $"{defName} is an HonorSymbol building but does not have honorProperties.";
        }

        if (!isHonorSymbol && honorProperties is not null)
        {
            yield return $"{defName} is not an HonorSymbol building but has honorProperties.";
        }
    }
}