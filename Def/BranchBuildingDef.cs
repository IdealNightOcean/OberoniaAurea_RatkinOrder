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
}